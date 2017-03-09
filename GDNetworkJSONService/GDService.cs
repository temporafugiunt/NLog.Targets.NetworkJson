using System;
using System.ServiceProcess;
using System.Threading;
using GDNetworkJSONService;
using GDNetworkJSONService.ExtensionMethods;
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

        private CommandLineModel _commandLineModel;

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
            var logger = LoggerFactory.GetInstrumentationLogger();
            logger.InitializeExecutionLogging($"{this.GetRealServiceName(ServiceName)} Startup");

            try
            {
                using (var dbConnection = LogStorageDbGlobals.OpenNewConnection())
                {
                    if (!LogStorageTable.TableExists(dbConnection))
                    {
                        logger.PushInfo($"{LogStorageTable.TableName} does not exist, creating.");
                        LogStorageTable.CreateTable(dbConnection);
                        logger.PushInfoWithTime($"{LogStorageTable.TableName} created.");
                    }
                    if (!DeadLetterLogStorageTable.TableExists(dbConnection))
                    {
                        logger.PushInfo($"{DeadLetterLogStorageTable.TableName} does not exist, creating.");
                        DeadLetterLogStorageTable.CreateTable(dbConnection);
                        logger.PushInfoWithTime($"{DeadLetterLogStorageTable.TableName} created.");
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
                logger.PushInfoWithTime("SignalR Web App Started.");
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
                logger.PushInfoWithTime("Guaranteed Delivery Thread Started.");

                _guaranteedDeliveryBackupThreadDelegate = new GuaranteedDeliveryThreadDelegate();

                thread = new Thread(() => GuaranteedDeliveryBackupThread.ThreadMethod(_guaranteedDeliveryBackupThreadDelegate));
                thread.Start();
                logger.PushInfoWithTime("Guaranteed Delivery Backup Thread Started.");
            }
            catch (Exception ex)
            {
                throw new Exception("Guaranteed Delivery Threads Initialization Failed.", ex);
            }

            logger.LogExecutionComplete(0);
        }

        protected override void OnStop()
        {
            var logger = LoggerFactory.GetInstrumentationLogger();
            var numMs = 0;
            logger.InitializeExecutionLogging($"{this.GetRealServiceName(ServiceName)} Shutdown");

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
