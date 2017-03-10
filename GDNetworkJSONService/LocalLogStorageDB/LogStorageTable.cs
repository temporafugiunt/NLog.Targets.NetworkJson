using System;
using System.Data;
using System.Data.SQLite;
using System.Net;
using NLog;
using NLog.Targets.NetworkJSON.ExtensionMethods;

namespace GDNetworkJSONService.LocalLogStorageDB
{
    internal class LogStorageTable
    {
        public const string TableName = "LogStorage";
        
        public class Columns
        {
            public static ColumnInfo MessageId { get; } = new ColumnInfo(nameof(MessageId), "INTEGER PRIMARY KEY ASC", DbType.Int64, 0);
            public static ColumnInfo Endpoint { get; } = new ColumnInfo(nameof(Endpoint), "NVARCHAR(1024)", DbType.String, 1);
            public static ColumnInfo LogMessage { get; } = new ColumnInfo(nameof(LogMessage), "TEXT", DbType.String, 2);
            public static ColumnInfo CreatedOn { get; } = new ColumnInfo(nameof(CreatedOn), "DATETIME", DbType.DateTime, 3);
            public static ColumnInfo RetryCount { get; } = new ColumnInfo(nameof(RetryCount), "INT2", DbType.Int16, 4);
        }

        public static bool TableExists(SQLiteConnection dbConnection)
        {
            var tableExistsSql = $"SELECT name FROM sqlite_master WHERE type='table' AND name='{TableName}'";
            var cmd = new SQLiteCommand(tableExistsSql, dbConnection);
            var tableName = cmd.ExecuteScalar()?.ToString();
            return (!tableName.IsNullOrEmpty());
        }
        
        public static void CreateTable(SQLiteConnection dbConnection)
        {
            var tableCreateSql = $"CREATE TABLE {TableName} ({Columns.MessageId.ColumnName} {Columns.MessageId.ColumnDDL}, {Columns.Endpoint.ColumnName} {Columns.Endpoint.ColumnDDL}, {Columns.LogMessage.ColumnName} {Columns.LogMessage.ColumnDDL}, {Columns.CreatedOn.ColumnName} {Columns.CreatedOn.ColumnDDL}, {Columns.RetryCount.ColumnName} {Columns.RetryCount.ColumnDDL})";
            var cmd = new SQLiteCommand(tableCreateSql, dbConnection);
            cmd.ExecuteNonQuery();
        }

        public static int InsertLogRecord(string endpoint, string logMessage)
        {
            using (var dbConnection = LogStorageDbGlobals.OpenNewConnection())
            {
                return InsertLogRecord(dbConnection, endpoint, logMessage);
            }
        }

        public static int InsertLogRecord(SQLiteConnection dbConnection, string endpoint, string logMessage)
        {
            var dataInsertSql = $"INSERT INTO {TableName} ({Columns.Endpoint.ColumnName}, {Columns.LogMessage.ColumnName}, {Columns.RetryCount.ColumnName}, {Columns.CreatedOn.ColumnName}) VALUES ({Columns.Endpoint.ParameterName}, {Columns.LogMessage.ParameterName}, {Columns.RetryCount.ParameterName}, {Columns.CreatedOn.ParameterName})";
            var cmd = new SQLiteCommand(dataInsertSql, dbConnection);

            var param = Columns.Endpoint.GetParamterForColumn();
            param.Value = endpoint;
            cmd.Parameters.Add(param);

            param = Columns.LogMessage.GetParamterForColumn();
            param.Value = logMessage;
            cmd.Parameters.Add(param);

            param = Columns.RetryCount.GetParamterForColumn();
            param.Value = 0;
            cmd.Parameters.Add(param);

            param = Columns.CreatedOn.GetParamterForColumn();
            param.Value = DateTime.Now;
            cmd.Parameters.Add(param);

            return cmd.ExecuteNonQuery();
        }

        public static DataTable GetFirstTryRecords(SQLiteConnection dbConnection)
        {
            var dataSelectSql = $"SELECT * FROM {TableName} WHERE {Columns.RetryCount.ColumnName} = 0 LIMIT {LogStorageDbGlobals.DbSelectCount}";
            var cmd = new SQLiteCommand(dataSelectSql, dbConnection);
            var dt = new DataTable(TableName);
            var reader = cmd.ExecuteReader();
            dt.Load(reader);
            return dt;
        }

        public static DataTable GetRetryRecords(SQLiteConnection dbConnection)
        {
            var dataSelectSql = $"SELECT * FROM {TableName} WHERE {Columns.RetryCount.ColumnName} > 0 ORDER BY RetryCount ASC, MessageId ASC LIMIT {LogStorageDbGlobals.DbSelectCount}";
            var cmd = new SQLiteCommand(dataSelectSql, dbConnection);
            var dt = new DataTable(TableName);
            var reader = cmd.ExecuteReader();
            dt.Load(reader);
            return dt;
        }

        public static int UpdateLogRecord(SQLiteConnection dbConnection, long messageId, long retryCount)
        {
            var dataInsertSql = $"UPDATE {TableName} SET {Columns.RetryCount.ColumnName} = {retryCount} WHERE {Columns.MessageId.ColumnName} = {messageId}";
            var cmd = new SQLiteCommand(dataInsertSql, dbConnection);
            
            return cmd.ExecuteNonQuery();
        }

        public static int DeleteProcessedRecord(SQLiteConnection dbConnection, long messageId)
        {
            var dataInsertSql = $"DELETE FROM {TableName} WHERE {Columns.MessageId.ColumnName} = {messageId}";
            var cmd = new SQLiteCommand(dataInsertSql, dbConnection);
            return cmd.ExecuteNonQuery();
        }

        public static long GetBacklogCount(SQLiteConnection dbConnection)
        {
            var dataSelectSql = $"SELECT COUNT(*) FROM {TableName}";
            var cmd = new SQLiteCommand(dataSelectSql, dbConnection);

            return (long)cmd.ExecuteScalar();
        }
    }
}
