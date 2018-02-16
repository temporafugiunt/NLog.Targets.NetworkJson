using System;
using System.Data;
using System.Data.SQLite;
using NLog.Targets.NetworkJSON.ExtensionMethods;
using NLog.Targets.NetworkJSON.LocalLogStorageDB;

namespace GDNetworkJSONService.LocalLogStorageDB
{
    internal class DeadLetterLogStorageTable
    {
        public const string TableName = "DeadLetterLogStorage";

        public class Columns
        {
            public static ColumnInfo Endpoint { get; } = new ColumnInfo(nameof(Endpoint), "NVARCHAR(1024)", DbType.String, 1);
            public static ColumnInfo EndpointType { get; } = new ColumnInfo(nameof(EndpointType), "NVARCHAR(20)", DbType.String, 2);
            public static ColumnInfo LogMessage { get; } = new ColumnInfo(nameof(LogMessage), "TEXT", DbType.String, 2);
            public static ColumnInfo CreatedOn { get; } = new ColumnInfo(nameof(CreatedOn), "DATETIME", DbType.DateTime, 3);
            public static ColumnInfo RetryCount { get; } = new ColumnInfo(nameof(RetryCount), "INT2", DbType.Int16, 4);
            public static ColumnInfo ArchivedOn { get; } = new ColumnInfo(nameof(ArchivedOn), "DATETIME", DbType.DateTime, 5);
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
            var tableCreateSql = $"CREATE TABLE {TableName} ({Columns.Endpoint.ColumnName} {Columns.Endpoint.ColumnDDL}, {Columns.EndpointType.ColumnName} {Columns.EndpointType.ColumnDDL}, {Columns.LogMessage.ColumnName} {Columns.LogMessage.ColumnDDL}, {Columns.CreatedOn.ColumnName} {Columns.CreatedOn.ColumnDDL}, {Columns.RetryCount.ColumnName} {Columns.RetryCount.ColumnDDL}, {Columns.ArchivedOn.ColumnName} {Columns.ArchivedOn.ColumnDDL})";
            var cmd = new SQLiteCommand(tableCreateSql, dbConnection);
            cmd.ExecuteNonQuery();
        }

        public static int InsertLogRecord(SQLiteConnection dbConnection, string endpoint, string endpointType, string logMessage, DateTime createdOn, long retryCount)
        {
            var dataInsertSql = $"INSERT INTO {TableName} ({Columns.Endpoint.ColumnName}, {Columns.EndpointType.ColumnName}, {Columns.LogMessage.ColumnName}, {Columns.CreatedOn.ColumnName}, {Columns.RetryCount.ColumnName}, {Columns.ArchivedOn.ColumnName}) VALUES ({Columns.Endpoint.ParameterName}, {Columns.EndpointType.ParameterName}, {Columns.LogMessage.ParameterName}, {Columns.CreatedOn.ParameterName}, {Columns.RetryCount.ParameterName}, {Columns.ArchivedOn.ParameterName})";
            var cmd = new SQLiteCommand(dataInsertSql, dbConnection);

            var param = Columns.Endpoint.GetParamterForColumn();
            param.Value = endpoint;
            cmd.Parameters.Add(param);

            param = Columns.EndpointType.GetParamterForColumn();
            param.Value = endpoint;
            cmd.Parameters.Add(param);

            param = Columns.LogMessage.GetParamterForColumn();
            param.Value = logMessage;
            cmd.Parameters.Add(param);

            param = Columns.CreatedOn.GetParamterForColumn();
            param.Value = createdOn;
            cmd.Parameters.Add(param);
            
            param = Columns.RetryCount.GetParamterForColumn();
            param.Value = retryCount;
            cmd.Parameters.Add(param);

            param = Columns.ArchivedOn.GetParamterForColumn();
            param.Value = DateTime.Now;
            cmd.Parameters.Add(param);

            return cmd.ExecuteNonQuery();
        }

        public static long GetDeadLetterCount(SQLiteConnection dbConnection)
        {
            var dataSelectSql = $"SELECT COUNT(*) FROM {TableName}";
            var cmd = new SQLiteCommand(dataSelectSql, dbConnection);
            
            return (long)cmd.ExecuteScalar();
        }
    }
}
