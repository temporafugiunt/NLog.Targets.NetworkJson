using System;
using System.Data;
using System.Data.SQLite;
using NLog.Targets.NetworkJSON.ExtensionMethods;

namespace GDNetworkJSONService.LocalLogStorageDB
{
    internal class LogStorageTable
    {
        public const string TableName = "LogStorage";
        
        public class Columns
        {
            public static ColumnInfo MessageId { get; } = new ColumnInfo("MessageId", "NVARCHAR(36)", DbType.String, 0);
            public static ColumnInfo Endpoint { get; } = new ColumnInfo("Endpoint", "NVARCHAR(1024)", DbType.String, 1);
            public static ColumnInfo LogMessage { get; } = new ColumnInfo("LogMessage", "TEXT", DbType.String, 2);
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
            var tableCreateSql = $"CREATE TABLE {TableName} ({Columns.MessageId.ColumnName} {Columns.MessageId.ColumnDDL}, {Columns.Endpoint.ColumnName} {Columns.Endpoint.ColumnDDL}, {Columns.LogMessage.ColumnName} {Columns.LogMessage.ColumnDDL})";
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
            var dataInsertSql = $"INSERT INTO {TableName} ({Columns.MessageId.ColumnName}, {Columns.Endpoint.ColumnName}, {Columns.LogMessage.ColumnName}) VALUES ({Columns.MessageId.ParameterName}, {Columns.Endpoint.ParameterName}, {Columns.LogMessage.ParameterName})";
            var cmd = new SQLiteCommand(dataInsertSql, dbConnection);

            var param = Columns.MessageId.GetParamterForColumn();
            param.Value = Guid.NewGuid().ToString();
            cmd.Parameters.Add(param);

            param = Columns.Endpoint.GetParamterForColumn();
            param.Value = endpoint;
            cmd.Parameters.Add(param);

            param = Columns.LogMessage.GetParamterForColumn();
            param.Value = logMessage;
            cmd.Parameters.Add(param);

            return cmd.ExecuteNonQuery();
        }

        public static DataTable GetNextTenRecords(SQLiteConnection dbConnection)
        {
            var dataSelectSql = $"SELECT * FROM {TableName} LIMIT {LogStorageDbGlobals.DbReadCount}";
            var cmd = new SQLiteCommand(dataSelectSql, dbConnection);
            var dt = new DataTable(TableName);
            var reader = cmd.ExecuteReader();
            dt.Load(reader);
            return dt;
        }

        public static int DeleteProcessedRecord(SQLiteConnection dbConnection, string messageId)
        {
            var dataInsertSql = $"DELETE FROM {TableName} WHERE {Columns.MessageId.ColumnName} = '{messageId}'";
            var cmd = new SQLiteCommand(dataInsertSql, dbConnection);
            return cmd.ExecuteNonQuery();
        }
    }
}
