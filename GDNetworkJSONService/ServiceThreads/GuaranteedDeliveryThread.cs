using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading;
using GDNetworkJSONService.LocalLogStorageDB;
using NLog.Targets.NetworkJSON;

namespace GDNetworkJSONService.ServiceThreads
{
    internal class GuaranteedDeliveryThreadDelegate
    {
        #region Public Properties

        public bool IsRunning { get; private set; }

        public bool IsAppShuttingDown { get; private set; }
        
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
            var targets = new Dictionary<string, NetworkJsonTarget>();
            while (!threadData.IsAppShuttingDown)
            {
                try
                {
                    if (dbConnection == null)
                    {
                        dbConnection = LogStorageDbGlobals.OpenNewConnection();
                    }

                    var logMessages = LogStorageTable.GetFirstTryRecords(dbConnection);
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
                            var logMessage = logMessages.Rows[inc][LogStorageTable.Columns.LogMessage.Index].ToString();
                            NetworkJsonTarget currentTarget = null;
                            if (!targets.TryGetValue(endpoint, out currentTarget))
                            {
                                currentTarget = new NetworkJsonTarget {Endpoint = endpoint};
                                targets.Add(endpoint, currentTarget);
                            }
                            try
                            {
                                currentTarget.Write(logMessage);
                                LogStorageTable.DeleteProcessedRecord(dbConnection, messageId);
                                Interlocked.Increment(ref TotalSuccessCount);
                            }
                            catch (Exception ex)
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
                catch (Exception ex)
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
