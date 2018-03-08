using System.Data;
using System.Data.SQLite;

namespace NLog.Targets.NetworkJSON.GuaranteedDelivery.LocalLogStorageDB
{
    public class ColumnInfo
    {
        public ColumnInfo(string columnName, string columnDDL, DbType parameterType, int columnIndex)
        {
            ColumnName = columnName;
            ColumnDDL = columnDDL;
            Index = columnIndex;
            ParameterName = $"@{columnName}";
            ParameterType = parameterType;
        }

        public string ColumnName { get; }
        public string ColumnDDL { get; }
        public int Index { get; }
        public string ParameterName { get; }
        public DbType ParameterType { get; }

        public SQLiteParameter GetParamterForColumn()
        {
            return new SQLiteParameter(ParameterName, ParameterType, ColumnName);
        }
    }
}
