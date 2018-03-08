using System;
using System.Data;
using System.Data.SQLite;
using NLog.Targets.NetworkJSON.ExtensionMethods;
using NLog.Targets.NetworkJSON.GuaranteedDelivery.LocalLogStorageDB;

namespace GDNetworkJSONService.LocalLogStorageDB
{
    internal class DeadLetterLogStorageTable
    {
        public const string TableName = "DeadLetterLogStorage";

        public enum ArchiveReasonId
        {
            MessageExpiration = 1,
            UnsupportedEndpointType = 2,
            MissingExtraInfo = 3,
            InvalidExtraInfo = 4,
            InvalidExtraInfoBadUnamePassword = 5
        }

        public class Columns
        {
            public static ColumnInfo Endpoint { get; } = new ColumnInfo(nameof(Endpoint), "NVARCHAR(1024)", DbType.String, 1);
            public static ColumnInfo EndpointType { get; } = new ColumnInfo(nameof(EndpointType), "NVARCHAR(20)", DbType.String, 2);
            public static ColumnInfo EndpointExtraInfo { get; } = new ColumnInfo(nameof(EndpointExtraInfo), "NVARCHAR(512)", DbType.String, 3);
            public static ColumnInfo LogMessage { get; } = new ColumnInfo(nameof(LogMessage), "TEXT", DbType.String, 2);
            public static ColumnInfo CreatedOn { get; } = new ColumnInfo(nameof(CreatedOn), "DATETIME", DbType.DateTime, 3);
            public static ColumnInfo RetryCount { get; } = new ColumnInfo(nameof(RetryCount), "INT2", DbType.Int16, 4);
            public static ColumnInfo ArchivedOn { get; } = new ColumnInfo(nameof(ArchivedOn), "DATETIME", DbType.DateTime, 5);
            public static ColumnInfo ArchiveReason { get; } = new ColumnInfo(nameof(ArchiveReason), "INT2", DbType.Int16, 4);
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
            var tableCreateSql = $"CREATE TABLE {TableName} ({Columns.Endpoint.ColumnName} {Columns.Endpoint.ColumnDDL}, {Columns.EndpointType.ColumnName} {Columns.EndpointType.ColumnDDL}, {Columns.EndpointExtraInfo.ColumnName} {Columns.EndpointExtraInfo.ColumnDDL}, {Columns.LogMessage.ColumnName} {Columns.LogMessage.ColumnDDL}, {Columns.CreatedOn.ColumnName} {Columns.CreatedOn.ColumnDDL}, {Columns.RetryCount.ColumnName} {Columns.RetryCount.ColumnDDL}, {Columns.ArchivedOn.ColumnName} {Columns.ArchivedOn.ColumnDDL}, {Columns.ArchiveReason.ColumnName} {Columns.ArchiveReason.ColumnDDL})";
            var cmd = new SQLiteCommand(tableCreateSql, dbConnection);
            cmd.ExecuteNonQuery();
        }

        public static int InsertLogRecord(SQLiteConnection dbConnection, string endpoint, string endpointType, string endpointExtraInfo, string logMessage, DateTime createdOn, long retryCount, int archiveReason)
        {
            var dataInsertSql = $"INSERT INTO {TableName} ({Columns.Endpoint.ColumnName}, {Columns.EndpointType.ColumnName}, {Columns.EndpointExtraInfo.ColumnName}, {Columns.LogMessage.ColumnName}, {Columns.CreatedOn.ColumnName}, {Columns.RetryCount.ColumnName}, {Columns.ArchivedOn.ColumnName}, {Columns.ArchiveReason.ColumnName}) VALUES ({Columns.Endpoint.ParameterName}, {Columns.EndpointType.ParameterName}, {Columns.EndpointExtraInfo.ParameterName}, {Columns.LogMessage.ParameterName}, {Columns.CreatedOn.ParameterName}, {Columns.RetryCount.ParameterName}, {Columns.ArchivedOn.ParameterName}, {Columns.ArchiveReason.ParameterName})";
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

            param = Columns.CreatedOn.GetParamterForColumn();
            param.Value = createdOn;
            cmd.Parameters.Add(param);
            
            param = Columns.RetryCount.GetParamterForColumn();
            param.Value = retryCount;
            cmd.Parameters.Add(param);

            param = Columns.ArchivedOn.GetParamterForColumn();
            param.Value = DateTime.Now;
            cmd.Parameters.Add(param);

            param = Columns.ArchiveReason.GetParamterForColumn();
            param.Value = archiveReason;
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
