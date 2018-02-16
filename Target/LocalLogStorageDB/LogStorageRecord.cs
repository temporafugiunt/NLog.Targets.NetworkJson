using System;

namespace NLog.Targets.NetworkJSON.LocalLogStorageDB
{
    public class LogStorageRecord
    {
        public Guid Id { get; set; }
        public string EndPoint { get; set; }
        public string LogMessage { get; set; }
    }
}
