using System;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NLog.Targets.NetworkJSON.ExtensionMethods;

namespace NLog.Targets.NetworkJSON.GuaranteedDelivery.LocalLogStorageDB
{
    public class LogStorageTable
    {
        public const string TableName = "LogStorage";
        
        public class Columns
        {
            public static ColumnInfo MessageId { get; } = new ColumnInfo(nameof(MessageId), "INTEGER PRIMARY KEY ASC", DbType.Int64, 0);
            public static ColumnInfo Endpoint { get; } = new ColumnInfo(nameof(Endpoint), "NVARCHAR(2048)", DbType.String, 1);
            public static ColumnInfo EndpointType { get; } = new ColumnInfo(nameof(EndpointType), "NVARCHAR(20)", DbType.String, 2);
            public static ColumnInfo EndpointExtraInfo { get; } = new ColumnInfo(nameof(EndpointExtraInfo), "NVARCHAR(512)", DbType.String, 3);
            public static ColumnInfo LogMessage { get; } = new ColumnInfo(nameof(LogMessage), "TEXT", DbType.String, 4);
            public static ColumnInfo CreatedOn { get; } = new ColumnInfo(nameof(CreatedOn), "DATETIME", DbType.DateTime, 5);
            public static ColumnInfo RetryCount { get; } = new ColumnInfo(nameof(RetryCount), "INT2", DbType.Int16, 6);
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
            var tableCreateSql = $"CREATE TABLE {TableName} ({Columns.MessageId.ColumnName} {Columns.MessageId.ColumnDDL}, {Columns.Endpoint.ColumnName} {Columns.Endpoint.ColumnDDL}, {Columns.EndpointType.ColumnName} {Columns.EndpointType.ColumnDDL}, {Columns.EndpointExtraInfo.ColumnName} {Columns.EndpointExtraInfo.ColumnDDL}, {Columns.LogMessage.ColumnName} {Columns.LogMessage.ColumnDDL}, {Columns.CreatedOn.ColumnName} {Columns.CreatedOn.ColumnDDL}, {Columns.RetryCount.ColumnName} {Columns.RetryCount.ColumnDDL})";
            var cmd = new SQLiteCommand(tableCreateSql, dbConnection);
            cmd.ExecuteNonQuery();
        }

        public static int InsertLogRecord(SQLiteConnection dbConnection, string endpoint, string endpointType, string endpointExtraInfo, string logMessage)
        {
            var cmd = BuildInsertCommand(dbConnection, endpoint, endpointType, endpointExtraInfo, logMessage);

            return cmd.ExecuteNonQuery();
        }

        public static async Task<int> InsertLogRecordAsync(SQLiteConnection dbConnection, string endpoint, string endpointType, string endpointExtraInfo, string logMessage)
        {
            var cmd = BuildInsertCommand(dbConnection, endpoint, endpointType, endpointExtraInfo, logMessage);

            return await cmd.ExecuteNonQueryAsync();
        }

        private static SQLiteCommand BuildInsertCommand(SQLiteConnection dbConnection, string endpoint, string endpointType, string endpointExtraInfo, string logMessage)
        {
            var dataInsertSql = $"INSERT INTO {TableName} ({Columns.Endpoint.ColumnName}, {Columns.EndpointType.ColumnName}, {Columns.EndpointExtraInfo.ColumnName}, {Columns.LogMessage.ColumnName}, {Columns.RetryCount.ColumnName}, {Columns.CreatedOn.ColumnName}) VALUES ({Columns.Endpoint.ParameterName}, {Columns.EndpointType.ParameterName}, {Columns.EndpointExtraInfo.ParameterName}, {Columns.LogMessage.ParameterName}, {Columns.RetryCount.ParameterName}, {Columns.CreatedOn.ParameterName})";
            var cmd = new SQLiteCommand(dataInsertSql, dbConnection);

            var param = Columns.Endpoint.GetParamterForColumn();
            param.Value = endpoint;
            cmd.Parameters.Add(param);

            param = Columns.EndpointType.GetParamterForColumn();
            param.Value = endpointType;
            cmd.Parameters.Add(param);

            param = Columns.EndpointExtraInfo.GetParamterForColumn();
            param.Value = endpointExtraInfo;
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

            return (cmd);
        }

        

        public static DataTable GetFirstTryRecords(SQLiteConnection dbConnection, int selectCount)
        {
            var dataSelectSql = $"SELECT * FROM {TableName} WHERE {Columns.RetryCount.ColumnName} = 0 LIMIT {selectCount}";
            var cmd = new SQLiteCommand(dataSelectSql, dbConnection);
            var dt = new DataTable(TableName);
            var reader = cmd.ExecuteReader();
            dt.Load(reader);
            return dt;
        }

        public static DataTable GetRetryRecords(SQLiteConnection dbConnection, int selectCount)
        {
            var dataSelectSql = $"SELECT * FROM {TableName} WHERE {Columns.RetryCount.ColumnName} > 0 ORDER BY RetryCount ASC, MessageId ASC LIMIT {selectCount}";
            var cmd = new SQLiteCommand(dataSelectSql, dbConnection);
            var dt = new DataTable(TableName);
            var reader = cmd.ExecuteReader();
            dt.Load(reader);
            return dt;
        }

        public static DataTable GetFailedRecords(SQLiteConnection dbConnection, int selectCount, int expiredMinutes)
        {
            var dataSelectSql = $"SELECT * FROM {TableName} WHERE {Columns.RetryCount.ColumnName} > 2 AND Cast((JulianDay() - JulianDay({Columns.CreatedOn.ColumnName})) * 24 * 60 As Integer) > {expiredMinutes} LIMIT {selectCount}";
            
            var cmd = new SQLiteCommand(dataSelectSql, dbConnection);
            var dt = new DataTable(TableName);
            var reader = cmd.ExecuteReader();
            dt.Load(reader);
            return dt;
        }

        public static int UpdateLogRecordRetryCount(SQLiteConnection dbConnection, long messageId)
        {
            var dataInsertSql = $"UPDATE {TableName} SET {Columns.RetryCount.ColumnName} = {Columns.RetryCount.ColumnName} + 1 WHERE {Columns.MessageId.ColumnName} = {messageId}";
            Debug.WriteLine(dataInsertSql);
            var cmd = new SQLiteCommand(dataInsertSql, dbConnection);
            
            return cmd.ExecuteNonQuery();
        }

        public static int UpdateLogRecordsRetryCount(SQLiteConnection dbConnection, long[] messageIds)
        {
            var dataInsertSql = $"UPDATE {TableName} SET {Columns.RetryCount.ColumnName} = {Columns.RetryCount.ColumnName} + 1 WHERE {Columns.MessageId.ColumnName} in ({string.Join(",", messageIds)})";
            Debug.WriteLine(dataInsertSql);
            var cmd = new SQLiteCommand(dataInsertSql, dbConnection);

            return cmd.ExecuteNonQuery();
        }

        public static int DeleteProcessedRecord(SQLiteConnection dbConnection, long messageId)
        {
            var dataInsertSql = $"DELETE FROM {TableName} WHERE {Columns.MessageId.ColumnName} = {messageId}";
            var cmd = new SQLiteCommand(dataInsertSql, dbConnection);
            return cmd.ExecuteNonQuery();
        }

        public static int DeleteProcessedRecords(SQLiteConnection dbConnection, long[] messageIds)
        {
            var dataInsertSql = $"DELETE FROM {TableName} WHERE {Columns.MessageId.ColumnName} in ({string.Join(",", messageIds)})";
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
