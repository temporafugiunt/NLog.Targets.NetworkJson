using System.Data.SQLite;

namespace GDNetworkJSONService.LocalLogStorageDB
{
    internal class LogStorageDbGlobals
    {
        public static int DbSelectCount { get; set; }

        public static int MinutesTillDeadLetter { get; set; }

        public static string GbDbsPath { get; set; }

        public static string GdDbConnectionStringTemplate { get; set; }
    }
}
