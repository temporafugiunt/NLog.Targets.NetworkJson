using System;
using System.ServiceProcess;
using System.Threading;
using GDNetworkJSONService;
using GDNetworkJSONService.ExtensionMethods;
using GDNetworkJSONService.Helpers;
using GDNetworkJSONService.Hubs;
using GDNetworkJSONService.LocalLogStorageDB;
using GDNetworkJSONService.Loggers;
using GDNetworkJSONService.Models;
using GDNetworkJSONService.ServiceThreads;
using Microsoft.Owin;
using Microsoft.Owin.Hosting;
using NLog.Targets.NetworkJSON.ExtensionMethods;

[assembly: OwinStartup(typeof(OwinStartup))]

namespace GDNetworkJSONService
{
    partial class GDService : ServiceBase
    {
        private IDisposable _signalR;
        private GuaranteedDeliveryThreadDelegate _guaranteedDeliveryThreadDelegate;
        private GuaranteedDeliveryThreadDelegate _guaranteedDeliveryBackupThreadDelegate;
        private bool _isRunning = true;
        private Timer _diagnosticsTimer;
        private long _diagnosticsInterval;

        private readonly MessageLogger _messageLogger = LoggerFactory.GetMessageLogger();
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
                using (var dbConnection = LogStorageDbGlobals.OpenNewConnection())
                {
                    if (!LogStorageTable.TableExists(dbConnection))
                    {
                        instrumentationlogger.PushInfo($"{LogStorageTable.TableName} does not exist, creating.");
                        LogStorageTable.CreateTable(dbConnection);
                        instrumentationlogger.PushInfoWithTime($"{LogStorageTable.TableName} created.");
                    }
                    if (!DeadLetterLogStorageTable.TableExists(dbConnection))
                    {
                        instrumentationlogger.PushInfo($"{DeadLetterLogStorageTable.TableName} does not exist, creating.");
                        DeadLetterLogStorageTable.CreateTable(dbConnection);
                        instrumentationlogger.PushInfoWithTime($"{DeadLetterLogStorageTable.TableName} created.");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("SQLite Database Opening / Creation Failed.", ex);
            }

            try
            {
                var options = new StartOptions();
                options.Urls.Add(_commandLineModel.Endpoint);
            
                _signalR = WebApp.Start(options);
                instrumentationlogger.PushInfoWithTime("SignalR Web App Started.");
            }
            catch (Exception ex)
            {
                throw new Exception("SignalR Hub Initialization Failed.", ex);
            }

            try
            {
                _guaranteedDeliveryThreadDelegate = new GuaranteedDeliveryThreadDelegate();

                var thread = new Thread(() => GuaranteedDeliveryThread.ThreadMethod(_guaranteedDeliveryThreadDelegate));
                thread.Start();
                instrumentationlogger.PushInfoWithTime("Guaranteed Delivery Thread Started.");

                _guaranteedDeliveryBackupThreadDelegate = new GuaranteedDeliveryThreadDelegate();

                thread = new Thread(() => GuaranteedDeliveryBackupThread.ThreadMethod(_guaranteedDeliveryBackupThreadDelegate));
                thread.Start();
                instrumentationlogger.PushInfoWithTime("Guaranteed Delivery Backup Thread Started.");

                SetupDiagnosticsSchedule();
            }
            catch (Exception ex)
            {
                throw new Exception("Guaranteed Delivery or Diagnostics Thread Initialization Failed.", ex);
            }

            instrumentationlogger.LogExecutionComplete(0);
        }

        private void SetupDiagnosticsSchedule()
        {
            try
            {
                if (!_isRunning) return;
                _diagnosticsTimer = new Timer(DiagnosticsCallback);
                
                var mode = AppSettingsHelper.DiagnosticsScheduleMode;
                _messageLogger.PushInfo($"Diagnostics Schedule Mode: {mode}");

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

                _messageLogger.PushInfo($"Diagnostics scheduled to run in: {schedule}");

                _messageLogger.PushInfo($"Diagnostics scheduled with an interval of: {_diagnosticsInterval} ms");

                _messageLogger.LogInfo("Diagnostics Thread Scheduled");

                //Get the difference in Minutes between the Scheduled and Current Time.
                var dueTime = Convert.ToInt32(timeSpan.TotalMilliseconds);

                //Change the Timer's Due Time.
                _diagnosticsTimer.Change(dueTime, _diagnosticsInterval);

            }
            catch (Exception ex)
            {
                _messageLogger.LogError("Diagnostics Thread Scheduling Error", ex);
            }
        }

        private void DiagnosticsCallback(object state)
        {
            if (!_isRunning) return;
            var diagnosticsLogger = LoggerFactory.GetDiagnosticsInstrumentationLogger();
            diagnosticsLogger.LogItemsSentFirstTry = Interlocked.Exchange(ref GuaranteedDeliveryThread.TotalSuccessCount, 0);
            diagnosticsLogger.LogItemsFailedFirstTry = Interlocked.Exchange(ref GuaranteedDeliveryThread.TotalFailedCount, 0);
            diagnosticsLogger.LogItemsSentOnRetry = Interlocked.Exchange(ref GuaranteedDeliveryBackupThread.TotalSuccessCount, 0);
            diagnosticsLogger.LogItemsFailedOnRetry = Interlocked.Exchange(ref GuaranteedDeliveryBackupThread.TotalFailedCount, 0);
            diagnosticsLogger.LogItemsReceived = Interlocked.Exchange(ref LoggingHub.TotalMessageCount, 0);
            diagnosticsLogger.DiagnosticsIntervalMS = _diagnosticsInterval;
            try
            {
                using (var dbConnection = LogStorageDbGlobals.OpenNewConnection())
                {
                    diagnosticsLogger.DeadLetterCount = DeadLetterLogStorageTable.GetDeadLetterCount(dbConnection);
                    diagnosticsLogger.BacklogCount = LogStorageTable.GetBacklogCount(dbConnection);
                }
            }
            catch (Exception ex)
            {
            }
            diagnosticsLogger.LogFullDiagnostics();
        }

        protected override void OnStop()
        {
            _diagnosticsTimer?.Dispose();
            var logger = LoggerFactory.GetInstrumentationLogger();
            var numMs = 0;
            logger.InitializeExecutionLogging($"{this.GetRealServiceName(ServiceName)} Shutdown");

            _isRunning = false;
            _signalR?.Dispose();
            _guaranteedDeliveryThreadDelegate.RegisterThreadShutdown();
            _guaranteedDeliveryBackupThreadDelegate.RegisterThreadShutdown();

            var inc = 0;
            while ((_guaranteedDeliveryThreadDelegate.IsRunning || _guaranteedDeliveryBackupThreadDelegate.IsRunning) && inc < 40)
            {
                numMs += 250;
                Thread.Sleep(250);
                inc++;
            }

            // Waited max of 10 seconds and it is still running
            if (inc == 40 && (_guaranteedDeliveryThreadDelegate.IsRunning || _guaranteedDeliveryBackupThreadDelegate.IsRunning))
            {
                logger.PushError("Guaranteed Delivery Thread(s) did not shut down in 10 seconds.");
                logger.LogExecutionCompleteAsError(failedItemCount: 0);
            }
            else
            {
                logger.PushInfo($"Guaranteed Delivery Thread(s) shut down in {numMs} milliseconds.");
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
