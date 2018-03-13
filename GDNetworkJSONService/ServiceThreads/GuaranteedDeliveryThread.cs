using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
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
    public class ThreadCounts
    {
        public int SuccessCount { get; set; }

        public int FailedCount { get; set; }

        public long BacklogCount { get; set; }

        public long DeadLetterCount { get; set; }
    }

    public class FullThreadDiagnosticInfo
    {
        public FullThreadDiagnosticInfo(ThreadCounts primaryThreadCounts, ThreadCounts backupThreadCounts, string loggingDb)
        {
            PrimaryThreadCounts = primaryThreadCounts;
            BackupThreadCounts = backupThreadCounts;
            LoggingDb = loggingDb;
        }

        public ThreadCounts PrimaryThreadCounts { get; }
        public ThreadCounts BackupThreadCounts { get; }
        public string LoggingDb { get; }
    }

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

        private readonly object _syncLock = new object();
        private ThreadCounts _counts { get; } = new ThreadCounts();

        #endregion

        public ThreadCounts GetCounts()
        {
            var localCounts = new ThreadCounts();
            lock (_syncLock)
            {
                localCounts.SuccessCount = _counts.SuccessCount;
                localCounts.FailedCount = _counts.FailedCount;
                localCounts.BacklogCount = _counts.BacklogCount;
                localCounts.DeadLetterCount = _counts.DeadLetterCount;
                _counts.SuccessCount = 0;
                _counts.FailedCount = 0;
            }
            return localCounts;
        }

        public void IncSuccess(int successIncCount)
        {
            lock (_syncLock)
            {
                _counts.SuccessCount += successIncCount;
            }
        }

        public void IncFailed(int failedIncCount)
        {
            lock (_syncLock)
            {
                _counts.FailedCount += failedIncCount;
            }
        }

        public void SetBacklogAndDLCounts(long backlogCount, long deadLetterCount)
        {
            lock (_syncLock)
            {
                _counts.BacklogCount = backlogCount;
                _counts.DeadLetterCount = deadLetterCount;
            }
        }

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
                        if (LogStorageDbGlobals.MultiWritePause > 0)
                        {
                            Thread.Sleep(LogStorageDbGlobals.MultiWritePause);
                        }
                        else
                        {
                            Thread.Sleep(1000);
                        }
                    }
                    else
                    {
                        // Group messages by endpoint so we can send groups of messages in the writers that support Multi Writes.
                        var endpointGroups = from logMessage in logMessages.AsEnumerable()
                            group logMessage by logMessage[LogStorageTable.Columns.Endpoint.Index];

                        foreach (var endpointGroup in endpointGroups)
                        {
                            var firstRow = endpointGroup.ElementAt(0);
                            var endpoint = firstRow[LogStorageTable.Columns.Endpoint.Index].ToString();
                            var endpointType = firstRow[LogStorageTable.Columns.EndpointType.Index].ToString();
                            var endpointExtraInfo = firstRow[LogStorageTable.Columns.EndpointExtraInfo.Index].ToString();

                            try
                            {
                                IGDEndpointWriter currentTarget;
                                if (!targets.TryGetValue($"{endpointType}_{endpoint}_{endpointExtraInfo}", out currentTarget))
                                {
                                    if (endpointType.CompareNoCase(GDServiceTarget.GDServiceTypes.socket))
                                    {
                                        currentTarget = new NetworkJsonTarget { Endpoint = endpoint };
                                    }
                                    else if (endpointType.CompareNoCase(GDServiceTarget.GDServiceTypes.elastic))
                                    {
                                        currentTarget = new ElasticEndpointWriter(endpoint, endpointExtraInfo);
                                    }
                                    else
                                    {
                                        throw new DeadLetterException(
                                            (int)DeadLetterLogStorageTable.ArchiveReasonId.UnsupportedEndpointType);
                                    }
                                    targets.Add($"{endpointType}_{endpoint}_{endpointExtraInfo}", currentTarget);
                                }
                                if (currentTarget.AllowMultiWrite)
                                {
                                    var messageIds = new List<long>();
                                    var logMessageStrings = new List<string>();
                                    foreach (var logMessageRow in endpointGroup)
                                    {
                                        messageIds.Add((long)logMessageRow[LogStorageTable.Columns.MessageId.Index]);
                                        logMessageStrings.Add(logMessageRow[LogStorageTable.Columns.LogMessage.Index].ToString());
                                    }
                                    try
                                    {
                                        Debug.WriteLine($"GD Thread Sending {logMessageStrings.Count} messages.");
                                        currentTarget.Write(logMessageStrings.ToArray());
                                        LogStorageTable.DeleteProcessedRecords(dbConnection, messageIds.ToArray());
                                        threadData.IncSuccess(messageIds.Count);
                                    }
                                    catch (Exception)
                                    {
                                        if (messageIds.Count > 0)
                                        {
                                            LogStorageTable.UpdateLogRecordsRetryCount(dbConnection, messageIds.ToArray());
                                            threadData.IncFailed(messageIds.Count);
                                        }
                                        throw;
                                    }
                                    // Pause to allow messages to "stack up" on client side so we have better write performance on client side
                                    // and larger packets on server side.
                                    if (LogStorageDbGlobals.MultiWritePause > 0 && messageIds.Count != LogStorageDbGlobals.DbSelectCount)
                                    {
                                        Debug.WriteLine($"Pausing GD Thread {LogStorageDbGlobals.MultiWritePause} MS to build up messages.");
                                        Thread.Sleep(LogStorageDbGlobals.MultiWritePause);
                                    }
                                }
                                else
                                {
                                    foreach (var logMessageRow in endpointGroup)
                                    {
                                        var messageId = (long)logMessageRow[LogStorageTable.Columns.MessageId.Index];
                                        var logMessage = logMessageRow[LogStorageTable.Columns.LogMessage.Index].ToString();

                                        try
                                        {
                                            currentTarget.Write(logMessage);
                                            LogStorageTable.DeleteProcessedRecord(dbConnection, messageId);
                                            threadData.IncSuccess(1);
                                        }
                                        catch (Exception)
                                        {
                                            // Fail the message, backup thread will take over for this message until dead letter time.
                                            LogStorageTable.UpdateLogRecordRetryCount(dbConnection, messageId);
                                            threadData.IncFailed(1);
                                            throw;
                                        }
                                    }
                                }
                            }
                            // This entire group is unsupported, this should only happen with target and service version conflicts or during development.
                            catch (DeadLetterException dlex)
                            {
                                foreach (var logMessageRow in endpointGroup)
                                {
                                    var messageId = (long)logMessageRow[LogStorageTable.Columns.MessageId.Index];
                                    var logMessage = logMessageRow[LogStorageTable.Columns.LogMessage.Index].ToString();
                                    var createdOn = (DateTime)logMessageRow[LogStorageTable.Columns.CreatedOn.Index];

                                    DeadLetterLogStorageTable.InsertLogRecord(dbConnection, endpoint, endpointType, endpointExtraInfo, logMessage, createdOn, 0, dlex.ArchiveReasonId);
                                    LogStorageTable.DeleteProcessedRecord(dbConnection, messageId);
                                    threadData.IncFailed(1);
                                }
                                
                            }
                            catch
                            {
                                targets.Remove(endpoint);
                                Thread.Sleep(500);
                            }
                        }
                    }
                    try
                    {
                        // Get current counts for diagnostic thread.
                        var backlogCount = LogStorageTable.GetBacklogCount(dbConnection);
                        var deadLetterCount = DeadLetterLogStorageTable.GetDeadLetterCount(dbConnection);
                        threadData.SetBacklogAndDLCounts(backlogCount, deadLetterCount);
                    }
                    catch {}
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
