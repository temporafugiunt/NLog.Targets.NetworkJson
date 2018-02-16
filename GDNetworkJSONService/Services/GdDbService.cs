using System;
using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GDNetworkJSONService.LocalLogStorageDB;
using GDNetworkJSONService.Loggers;
using GDNetworkJSONService.ServiceThreads;
using Microsoft.Owin.Hosting;
using NLog.Targets.NetworkJSON.LocalLogStorageDB;

namespace GDNetworkJSONService.Services
{
    public class GdDbService
    {
        private GuaranteedDeliveryThreadDelegate _guaranteedDeliveryThreadDelegate;
        private GuaranteedDeliveryThreadDelegate _guaranteedDeliveryBackupThreadDelegate;

        public GdDbService(string serviceName, string dbFilePath)
        {
            DbFilePath = dbFilePath;
            ServiceName = serviceName;
        }

        #region Public Properties

        public string DbFilePath { get; }
        public string ServiceName { get; }
        
        public bool IsRunning
        {
            get
            {
                if (_guaranteedDeliveryThreadDelegate != null)
                {
                    if (_guaranteedDeliveryThreadDelegate.IsRunning) return true;
                }
                if (_guaranteedDeliveryBackupThreadDelegate != null)
                {
                    if (_guaranteedDeliveryBackupThreadDelegate.IsRunning) return true;
                }
                return false;
            }
        }

        public bool IsAppShuttingDown { get; private set; }

        #endregion

        public void StartServiceThreads()
        {
            var instrumentationlogger = LoggerFactory.GetInstrumentationLogger();
            instrumentationlogger.InitializeExecutionLogging($"{ServiceName} Startup");

            try
            {
                using (var dbConnection = LogStorageConnection.OpenConnection(DbFilePath))
                {
                    // Build proper DB schema if not there... At least the LogStorageTable should be there because
                    // NLog thread should have created that, but whateva!
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

                _guaranteedDeliveryThreadDelegate = new GuaranteedDeliveryThreadDelegate(DbFilePath);

                var thread = new Thread(() => GuaranteedDeliveryThread.ThreadMethod(_guaranteedDeliveryThreadDelegate));
                thread.Start();
                instrumentationlogger.PushInfoWithTime($"Guaranteed Delivery Thread Started for '{DbFilePath}'.");

                _guaranteedDeliveryBackupThreadDelegate = new GuaranteedDeliveryThreadDelegate(DbFilePath);

                thread = new Thread(() => GuaranteedDeliveryBackupThread.ThreadMethod(_guaranteedDeliveryBackupThreadDelegate));
                thread.Start();
                instrumentationlogger.PushInfoWithTime($"Guaranteed Delivery Backup Thread Started for '{DbFilePath}'.");
            }
            catch (Exception ex)
            {
                throw new Exception("SQLite Database Opening / Creation Failed.", ex);
            }

            instrumentationlogger.LogExecutionComplete(0);
        }

        public void RegisterServiceShutdown()
        {
            IsAppShuttingDown = true;
            _guaranteedDeliveryThreadDelegate?.RegisterThreadShutdown();
            _guaranteedDeliveryBackupThreadDelegate?.RegisterThreadShutdown();
        }
    }
}
