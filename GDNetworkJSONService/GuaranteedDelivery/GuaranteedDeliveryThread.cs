using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading;
using GDNetworkJSONService.LocalLogStorageDB;
using NLog.Targets.NetworkJSON;

namespace GDNetworkJSONService.GuaranteedDelivery
{
    internal class GuaranteedDeliveryThreadDelegate
    {
        #region Public Properties

        public bool IsRunning { get; private set; } = true;

        public bool IsAppShuttingDown { get; private set; } = false;

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
        public static int TotalMessageCount;

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

                    var logMessages = LogStorageTable.GetNextTenRecords(dbConnection);
                    if (logMessages.Rows.Count == 0)
                    {
                        Thread.Sleep(500);
                    }
                    else
                    {
                        for (var inc = 0; inc < logMessages.Rows.Count; inc++)
                        {
                            var messageId = logMessages.Rows[inc][LogStorageTable.Columns.MessageId.Index].ToString();
                            var endpoint = logMessages.Rows[inc][LogStorageTable.Columns.Endpoint.Index].ToString();
                            var logMessage = logMessages.Rows[inc][LogStorageTable.Columns.LogMessage.Index].ToString();
                            NetworkJsonTarget currentTarget = null;
                            if (!targets.TryGetValue(endpoint, out currentTarget))
                            {
                                currentTarget = new NetworkJsonTarget {Endpoint = endpoint};
                                targets.Add(endpoint, currentTarget);
                            }
                            currentTarget.Write(logMessage);
                            LogStorageTable.DeleteProcessedRecord(dbConnection, messageId);
                            Interlocked.Increment(ref TotalMessageCount);
                            Console.WriteLine($"OUT={TotalMessageCount}");
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
            Console.WriteLine("Background Thread Shutdown");
            threadData.ThreadHasShutdown();
        }
    }
}
