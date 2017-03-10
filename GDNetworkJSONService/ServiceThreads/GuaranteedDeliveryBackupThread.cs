using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading;
using GDNetworkJSONService.LocalLogStorageDB;
using NLog.Targets.NetworkJSON;

namespace GDNetworkJSONService.ServiceThreads
{
    internal class GuaranteedDeliveryBackupThread
    {
        public static int TotalSuccessCount;
        public static int TotalFailedCount;

        public static void ThreadMethod(GuaranteedDeliveryThreadDelegate threadData)
        {
            SQLiteConnection dbConnection = null;
            var targets = new Dictionary<string, NetworkJsonTarget>();
            while (!threadData.IsAppShuttingDown)
            {
                try
                {
                    if (dbConnection == null)
                    {
                        dbConnection = LogStorageDbGlobals.OpenNewConnection();
                    }

                    var logMessages = LogStorageTable.GetRetryRecords(dbConnection);
                    if (logMessages.Rows.Count == 0)
                    {
                        Thread.Sleep(60000);
                    }
                    else
                    {
                        for (var inc = 0; inc < logMessages.Rows.Count; inc++)
                        {
                            var messageId = (long)logMessages.Rows[inc][LogStorageTable.Columns.MessageId.Index];
                            var endpoint = logMessages.Rows[inc][LogStorageTable.Columns.Endpoint.Index].ToString();
                            var logMessage = logMessages.Rows[inc][LogStorageTable.Columns.LogMessage.Index].ToString();
                            var retryCount = (long)logMessages.Rows[inc][LogStorageTable.Columns.RetryCount.Index];
                            var createdOn = (DateTime) logMessages.Rows[inc][LogStorageTable.Columns.CreatedOn.Index];
                            NetworkJsonTarget currentTarget = null;
                            if (!targets.TryGetValue(endpoint, out currentTarget))
                            {
                                currentTarget = new NetworkJsonTarget {Endpoint = endpoint};
                                targets.Add(endpoint, currentTarget);
                            }
                            try
                            {
                                retryCount++;
                                currentTarget.Write(logMessage);
                                LogStorageTable.DeleteProcessedRecord(dbConnection, messageId);
                                Interlocked.Increment(ref TotalSuccessCount);
                            }
                            catch (Exception ex)
                            {
                                var recordAge = DateTime.Now - createdOn;
                                if (recordAge.Minutes > LogStorageDbGlobals.MinutesTillDeadLetter)
                                {
                                    DeadLetterLogStorageTable.InsertLogRecord(dbConnection, endpoint, logMessage, createdOn, retryCount);
                                    LogStorageTable.DeleteProcessedRecord(dbConnection, messageId);
                                }
                                else
                                {
                                    LogStorageTable.UpdateLogRecord(dbConnection, messageId, retryCount);
                                }
                                targets.Remove(endpoint);
                                Interlocked.Increment(ref TotalFailedCount);
                                Thread.Sleep(2000);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    dbConnection?.Close();
                    dbConnection = null;
                    targets.Clear();
                    Thread.Sleep(60000);
                }
                
            }
            threadData.ThreadHasShutdown();
        }
    }
}
