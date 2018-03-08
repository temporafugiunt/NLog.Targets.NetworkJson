using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading;
using GDNetworkJSONService.Exceptions;
using GDNetworkJSONService.GDEndpointWriters;
using GDNetworkJSONService.LocalLogStorageDB;
using NLog.Targets.NetworkJSON;
using NLog.Targets.NetworkJSON.ExtensionMethods;
using NLog.Targets.NetworkJSON.GuaranteedDelivery;
using NLog.Targets.NetworkJSON.GuaranteedDelivery.LocalLogStorageDB;

namespace GDNetworkJSONService.ServiceThreads
{
    internal class GuaranteedDeliveryThreadDelegate
    {
        public GuaranteedDeliveryThreadDelegate(string dbFilePath)
        {
            DbFilePath = dbFilePath;
        }

        #region Public Properties

        public bool IsRunning { get; private set; }

        public bool IsAppShuttingDown { get; private set; }

        public string DbFilePath { get; }

        #endregion

        public void RegisterThreadShutdown()
        {
            IsAppShuttingDown = true;
        }

        public void ThreadHasShutdown()
        {
            IsRunning = false;
        }
    }

    internal class GuaranteedDeliveryThread
    {
        public static int TotalSuccessCount;
        public static int TotalFailedCount;

        public static void ThreadMethod(GuaranteedDeliveryThreadDelegate threadData)
        {
            SQLiteConnection dbConnection = null;
            var targets = new Dictionary<string, IGDEndpointWriter>();
            while (!threadData.IsAppShuttingDown)
            {
                try
                {
                    if (dbConnection == null)
                    {
                        dbConnection = LogStorageConnection.OpenConnection(threadData.DbFilePath);
                    }

                    var logMessages = LogStorageTable.GetFirstTryRecords(dbConnection, LogStorageDbGlobals.DbSelectCount);
                    if (logMessages.Rows.Count == 0)
                    {
                        Thread.Sleep(500);
                    }
                    else
                    {
                        for (var inc = 0; inc < logMessages.Rows.Count; inc++)
                        {
                            var messageId = (long)logMessages.Rows[inc][LogStorageTable.Columns.MessageId.Index];
                            var endpoint = logMessages.Rows[inc][LogStorageTable.Columns.Endpoint.Index].ToString();
                            var endpointType = logMessages.Rows[inc][LogStorageTable.Columns.EndpointType.Index].ToString();
                            var endpointExtraInfo = logMessages.Rows[inc][LogStorageTable.Columns.EndpointExtraInfo.Index].ToString();
                            var logMessage = logMessages.Rows[inc][LogStorageTable.Columns.LogMessage.Index].ToString();
                            var createdOn = (DateTime)logMessages.Rows[inc][LogStorageTable.Columns.CreatedOn.Index];

                            try
                            {
                                IGDEndpointWriter currentTarget;
                                if (!targets.TryGetValue(endpoint, out currentTarget))
                                {
                                    if (endpointType.CompareNoCase(GDServiceTarget.GDServiceTypes.socket))
                                    {
                                        currentTarget = new NetworkJsonTarget {Endpoint = endpoint};
                                    }
                                    else if (endpointType.CompareNoCase(GDServiceTarget.GDServiceTypes.elastic))
                                    {
                                        currentTarget = new ElasticEndpointWriter(endpoint, endpointExtraInfo);
                                    }
                                    else
                                    {
                                        throw new DeadLetterException(
                                            (int) DeadLetterLogStorageTable.ArchiveReasonId.UnsupportedEndpointType);
                                    }
                                    targets.Add(endpoint, currentTarget);
                                }
                                currentTarget.Write(logMessage);
                                LogStorageTable.DeleteProcessedRecord(dbConnection, messageId);
                                Interlocked.Increment(ref TotalSuccessCount);
                            }
                            catch (DeadLetterException dlex)
                            {
                                DeadLetterLogStorageTable.InsertLogRecord(dbConnection, endpoint, endpointType, endpointExtraInfo, logMessage, createdOn, 0, dlex.ArchiveReasonId);
                                LogStorageTable.DeleteProcessedRecord(dbConnection, messageId);
                            }
                            catch
                            {
                                // Fail the message, backup thread will take over for this message until dead letter time.
                                LogStorageTable.UpdateLogRecord(dbConnection, messageId, 1);
                                targets.Remove(endpoint);
                                Interlocked.Increment(ref TotalFailedCount);
                                Thread.Sleep(500);
                            }
                        }
                    }
                }
                catch
                {
                    dbConnection?.Close();
                    dbConnection = null;
                    targets.Clear();
                    Thread.Sleep(1000);
                }
            }
            threadData.ThreadHasShutdown();
        }
    }
}
