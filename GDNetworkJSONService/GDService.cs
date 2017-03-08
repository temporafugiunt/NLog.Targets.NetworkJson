using System;
using System.ServiceProcess;
using System.Threading;
using GDNetworkJSONService;
using GDNetworkJSONService.ExtensionMethods;
using GDNetworkJSONService.GuaranteedDelivery;
using GDNetworkJSONService.LocalLogStorageDB;
using GDNetworkJSONService.Loggers;
using GDNetworkJSONService.Models;
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

        private CommandLineModel _commandLineModel;

        public GDService(CommandLineModel model)
        {
            _commandLineModel = model;
            ServiceName = "NetworkJSON Guaranteed Delivery Service";
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
                using (var dbConnection = LocalLogStorageDB.LogStorageDbGlobals.OpenNewConnection())
                {
                    if (!LogStorageTable.TableExists(dbConnection))
                    {
                        logger.PushInfoWithTime($"{LogStorageTable.TableName} does not exist, creating.");
                        LogStorageTable.CreateTable(dbConnection);
                        logger.PushInfoWithTime($"{LogStorageTable.TableName} created.");
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
            }
            catch (Exception ex)
            {
                throw new Exception("Guaranteed Delivery Thread Initialization Failed.", ex);
            }

            logger.LogExecutionComplete(0);
        }

        

        protected override void OnStop()
        {
            var logger = LoggerFactory.GetInstrumentationLogger();
            logger.InitializeExecutionLogging($"{this.GetRealServiceName(ServiceName)} Shutdown");

            _signalR?.Dispose();
            _guaranteedDeliveryThreadDelegate.RegisterThreadShutdown();
            var inc = 0;
            while (_guaranteedDeliveryThreadDelegate.IsRunning && inc < 40)
            {
                logger.PushInfo("Pausing 250 ms waiting for Quaranteed Delivery Thread to shutdown...");
                Thread.Sleep(250);
                inc++;
            }

            // Waited max of 10 seconds and it is still running
            if (inc == 40 && _guaranteedDeliveryThreadDelegate.IsRunning)
            {
                logger.PushError("Quaranteed Delivery Thread did not shut down in 10 seconds.");
                logger.LogExecutionCompleteAsError(failedItemCount: 0);
            }
            else
            {
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
