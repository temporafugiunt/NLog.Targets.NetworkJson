namespace NLog.Targets.NetworkJSON.LoadTester.Models
{
    public struct SignalRCallData
    {
        public string TestDataName;
        public bool CallFailed;
        public long CallTimeMS;
        public long TotalBytes;
        public double BytesPerMS;
    }
}
