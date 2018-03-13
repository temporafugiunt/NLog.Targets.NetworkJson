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
                                    }
                                    catch (Exception)
                                    {
                                        if(messageIds.Count > 0)
                                        LogStorageTable.UpdateLogRecordsRetryCount(dbConnection, messageIds.ToArray());
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
                                            Interlocked.Increment(ref TotalSuccessCount);
                                        }
                                        catch (Exception)
                                        {
                                            // Fail the message, backup thread will take over for this message until dead letter time.
                                            LogStorageTable.UpdateLogRecordRetryCount(dbConnection, messageId);
                                            throw;
                                        }
                                    }
                                }
                                Interlocked.Increment(ref TotalSuccessCount);
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
                                }
                                
                            }
                            catch
                            {
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
