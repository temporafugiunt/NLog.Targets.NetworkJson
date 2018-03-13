using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using GDNetworkJSONService;
using GDNetworkJSONService.ExtensionMethods;
using GDNetworkJSONService.Helpers;
using GDNetworkJSONService.LocalLogStorageDB;
using GDNetworkJSONService.Loggers;
using GDNetworkJSONService.Models;
using GDNetworkJSONService.Services;
using GDNetworkJSONService.ServiceThreads;
using Microsoft.Owin;
using NLog.Targets.NetworkJSON.ExtensionMethods;

[assembly: OwinStartup(typeof(OwinStartup))]

namespace GDNetworkJSONService
{
    partial class GDService : ServiceBase
    {
        private bool _isRunning = true;
        private Timer _diagnosticsTimer;
        private long _diagnosticsInterval;
        private Dictionary<string, GdDbService> _dbThreads = new Dictionary<string, GdDbService>();  

        private readonly CommandLineModel _commandLineModel;

        public GDService(CommandLineModel model)
        {
            _commandLineModel = model;
            ServiceName = "Guaranteed Delivery NetworkJSON Service";
            InitializeComponent();
        }

        public void OnStartConsoleMode()
        {
            OnStart(null);
        }

        public void OnStopConsoleMode()
        {
            OnStop();
        }

        protected override void OnStart(string[] args)
        {
            var instrumentationlogger = LoggerFactory.GetInstrumentationLogger();
            instrumentationlogger.InitializeExecutionLogging($"{this.GetRealServiceName(ServiceName)} Startup");

            try
            {
                var configLogger = LoggerFactory.GetConfigLogger(false);
                configLogger.LogConfigSettings();

                // Setup initial DB threads based on startup list of GD Logging Databases found.
                MaintainGdDbList();

                // The diagnostics thread also runs MaintainGdDbList() as its last operation to service Dbs that appear 
                // after this service has already started up.
                SetupDiagnosticsSchedule();
            }
            catch (Exception ex)
            {
                throw new Exception($"{this.GetRealServiceName(ServiceName)} base initialization failure.", ex);
            }
            
            instrumentationlogger.LogExecutionComplete(0);
        }

        private void SetupDiagnosticsSchedule()
        {
            var messageLogger = LoggerFactory.GetMessageLogger();
            try
            {
                if (!_isRunning) return;
                _diagnosticsTimer = new Timer(DiagnosticsCallback);
                
                var mode = AppSettingsHelper.DiagnosticsScheduleMode;
                messageLogger.PushInfo($"Diagnostics Schedule Mode: {mode}");

                //Set the Default Time.
                var scheduledTime = DateTime.MinValue;

                if (mode.ToUpper() == "DAILY")
                {
                    //Get the Scheduled Time from AppSettings.
                    scheduledTime = AppSettingsHelper.DiagnosticsScheduledTime;
                    if (DateTime.Now > scheduledTime)
                    {
                        //If Scheduled Time is passed set Schedule for the next day.
                        scheduledTime = scheduledTime.AddDays(1);
                    }
                    _diagnosticsInterval = (24 * 60 * 60 * 1000);
                }

                if (mode.ToUpper() == "INTERVAL")
                {
                    //Get the Interval in Seconds from AppSettings.
                    var intervalSeconds = AppSettingsHelper.DiagnosticsIntervalSeconds;
                    //Set the Scheduled Time by adding the Interval to Current Time.
                    scheduledTime = DateTime.Now.AddSeconds(intervalSeconds);
                    if (DateTime.Now > scheduledTime)
                    {
                        //If Scheduled Time is passed set Schedule for the next Interval.
                        scheduledTime = scheduledTime.AddMinutes(intervalSeconds);
                    }
                    _diagnosticsInterval = (intervalSeconds * 1000);
                }

                var timeSpan = scheduledTime.Subtract(DateTime.Now);
                var schedule = $"{timeSpan.Days} day(s) {timeSpan.Hours} hour(s) {timeSpan.Minutes} minute(s) {timeSpan.Seconds} seconds(s)";

                messageLogger.PushInfo($"Diagnostics scheduled to run in: {schedule}");

                messageLogger.PushInfo($"Diagnostics scheduled with an interval of: {_diagnosticsInterval} ms");

                messageLogger.LogInfo("Diagnostics Thread Scheduled");

                //Get the difference in Minutes between the Scheduled and Current Time.
                var dueTime = Convert.ToInt32(timeSpan.TotalMilliseconds);

                //Change the Timer's Due Time.
                _diagnosticsTimer.Change(dueTime, _diagnosticsInterval);

            }
            catch (Exception ex)
            {
                messageLogger.LogError("Diagnostics Thread Scheduling Error", ex);
            }
        }

        private void MaintainGdDbList()
        {
            lock (_dbThreads)
            {
                var currentGdDbs = GdDbHelper.GetGdDbListFromDirectory(LogStorageDbGlobals.GdDbsPath);
                foreach (var gdDb in currentGdDbs)
                {
                    if (_dbThreads.ContainsKey(gdDb)) continue;
                    // New DB has come online.
                    var gdDbService = new GdDbService(this.GetRealServiceName(ServiceName), gdDb);
                    gdDbService.StartServiceThreads();
                    _dbThreads.Add(gdDb, gdDbService);
                }
            }
        }

         

        private void DiagnosticsCallback(object state)
        {
            if (!_isRunning) return;

            var threadDiagnostics = new List<FullThreadDiagnosticInfo>();

            lock (_dbThreads)
            {
                foreach (var gdDbService in _dbThreads.Values)
                {
                    // Don't publish metadata about your metadata... That is just too meta... data...
                    if (gdDbService.DbFilePath.ToLower().Contains("gddiagnostics.sqlite")) continue;
                    threadDiagnostics.Add(new FullThreadDiagnosticInfo(gdDbService.GetPrimaryThreadCounts(), gdDbService.GetBackupThreadCounts(), gdDbService.DbFilePath));
                }
            }

            var diagnosticsLogger = LoggerFactory.GetDiagnosticsInstrumentationLogger();

            // Send a diagnostic message per thread.
            foreach (var threadDiagnostic in threadDiagnostics)
            {
                diagnosticsLogger.LoggingDB = threadDiagnostic.LoggingDb;
                diagnosticsLogger.LogItemsSentFirstTry = threadDiagnostic.PrimaryThreadCounts.SuccessCount;
                diagnosticsLogger.LogItemsFailedFirstTry = threadDiagnostic.PrimaryThreadCounts.FailedCount;
                diagnosticsLogger.BacklogCount = threadDiagnostic.PrimaryThreadCounts.BacklogCount;
                diagnosticsLogger.DeadLetterCount = threadDiagnostic.PrimaryThreadCounts.DeadLetterCount;
                diagnosticsLogger.LogItemsSentOnRetry = threadDiagnostic.BackupThreadCounts.SuccessCount;
                diagnosticsLogger.LogItemsFailedOnRetry = threadDiagnostic.BackupThreadCounts.FailedCount;
                diagnosticsLogger.DiagnosticsIntervalMS = _diagnosticsInterval;

                if (AppSettingsHelper.SkipZeroDiagnostics)
                {
                    if (diagnosticsLogger.LogItemsSentFirstTry > 0 ||diagnosticsLogger.LogItemsFailedFirstTry > 0 || 
                        diagnosticsLogger.LogItemsSentOnRetry > 0 || diagnosticsLogger.LogItemsFailedOnRetry > 0 || 
                        diagnosticsLogger.BacklogCount > 0 || diagnosticsLogger.DeadLetterCount > 0)
                    {
                        diagnosticsLogger.LogFullDiagnostics();
                    }
                }
                else
                {
                    diagnosticsLogger.LogFullDiagnostics();
                }
            }

            MaintainGdDbList();
        }

        protected override void OnStop()
        {
            _diagnosticsTimer?.Dispose();
            var logger = LoggerFactory.GetInstrumentationLogger();
            var numMs = 0;
            logger.InitializeExecutionLogging($"{this.GetRealServiceName(ServiceName)} Shutdown");

            _isRunning = false;
            var runningServiceCount = 1;

            lock (_dbThreads)
            {
                foreach (var gdDbService in _dbThreads.Values)
                {
                    gdDbService.RegisterServiceShutdown();
                }
            }

            var inc = 0;
            while ((runningServiceCount > 0) && inc < 40)
            {
                numMs += 250;
                Thread.Sleep(250);
                lock (_dbThreads)
                {
                    var shutdownServiceKeys = new List<string>();
                    foreach (var gdDbService in _dbThreads)
                    {
                        if (!gdDbService.Value.IsRunning)
                        {
                            shutdownServiceKeys.Add(gdDbService.Key);
                        }
                    }
                    foreach (string serviceKey in shutdownServiceKeys)
                    {
                        _dbThreads.Remove(serviceKey);
                    }
                    runningServiceCount = _dbThreads.Count;
                }
                inc++;
            }

            // Waited max of 10 seconds and it is still running
            if (inc == 40 && runningServiceCount > 0)
            {
                logger.PushError("Guaranteed Delivery Database Service(s) did not shut down in 10 seconds.");
                logger.LogExecutionCompleteAsError(failedItemCount: 0);
            }
            else
            {
                logger.PushInfo($"Guaranteed Delivery Database Service(s) shut down in {numMs} milliseconds.");
                logger.LogExecutionComplete(0);
            }
        }

        private string _realServiceName;
        public string RealServiceName
        {
            get
            {
                if (_realServiceName.IsNullOrEmpty())
                {
                    _realServiceName = this.GetRealServiceName(ServiceName);
                }
                return(_realServiceName);
            }
        }
    }
}
