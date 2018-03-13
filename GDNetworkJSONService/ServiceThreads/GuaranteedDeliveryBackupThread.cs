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
    internal class GuaranteedDeliveryBackupThread
    {
        public static int TotalSuccessCount;
        public static int TotalFailedCount;

        public string DbFilePath { get; }

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

                    var logMessages = LogStorageTable.GetRetryRecords(dbConnection, LogStorageDbGlobals.DbSelectCount);
                    if (logMessages.Rows.Count == 0)
                    {
                        Thread.Sleep(60000);
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
                                        throw new DeadLetterException((int)DeadLetterLogStorageTable.ArchiveReasonId.UnsupportedEndpointType);
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
                                        Debug.WriteLine($"GDBackground Thread Sending {logMessageStrings.Count} messages.");
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

                        try
                        {
                            // Find and move any dead letters
                            if (LogStorageDbGlobals.MinutesTillDeadLetter <= 0) continue;
                            var failedLogMessages = LogStorageTable.GetFailedRecords(dbConnection, LogStorageDbGlobals.DbSelectCount, LogStorageDbGlobals.MinutesTillDeadLetter);
                            for (var inc = 0; inc < failedLogMessages.Rows.Count; inc++)
                            {
                                var messageId = (long)failedLogMessages.Rows[inc][LogStorageTable.Columns.MessageId.Index];
                                var endpoint = failedLogMessages.Rows[inc][LogStorageTable.Columns.Endpoint.Index].ToString();
                                var endpointType = failedLogMessages.Rows[inc][LogStorageTable.Columns.EndpointType.Index].ToString();
                                var endpointExtraInfo = failedLogMessages.Rows[inc][LogStorageTable.Columns.EndpointExtraInfo.Index].ToString();
                                var logMessage = failedLogMessages.Rows[inc][LogStorageTable.Columns.LogMessage.Index].ToString();
                                var retryCount = (long)failedLogMessages.Rows[inc][LogStorageTable.Columns.RetryCount.Index];
                                var createdOn = (DateTime)failedLogMessages.Rows[inc][LogStorageTable.Columns.CreatedOn.Index];

                                DeadLetterLogStorageTable.InsertLogRecord(dbConnection, endpoint, endpointType, endpointExtraInfo, logMessage, createdOn, retryCount, (int)DeadLetterLogStorageTable.ArchiveReasonId.MessageExpiration);
                                LogStorageTable.DeleteProcessedRecord(dbConnection, messageId);
                            }
                        }
                        catch {}
                    }
                }
                catch
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
