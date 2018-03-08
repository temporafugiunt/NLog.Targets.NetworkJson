using System;

namespace NLog.Targets.NetworkJSON.GuaranteedDelivery.LocalLogStorageDB
{
    public class LogStorageRecord
    {
        public Guid Id { get; set; }
        public string EndPoint { get; set; }
        public string LogMessage { get; set; }
    }
}
