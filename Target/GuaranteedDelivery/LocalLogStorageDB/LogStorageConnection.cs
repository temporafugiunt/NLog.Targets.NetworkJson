using System.Data.SQLite;

namespace NLog.Targets.NetworkJSON.GuaranteedDelivery.LocalLogStorageDB
{
    public class LogStorageConnection
    {
        public static SQLiteConnection OpenConnection(string dbFileName)
        {
            var dbConnection = new SQLiteConnection($"Data Source={dbFileName};Version=3;Pooling=True;");
            dbConnection.Open();
            return dbConnection;
        }
    }
}
