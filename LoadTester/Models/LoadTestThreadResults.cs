using System;
using System.Collections.Generic;

namespace NLog.Targets.NetworkJSON.LoadTester.Models
{
    public class LoadTestThreadResults
    {
        public string ClientStartTime { get; }
        public int ThreadID { get; }
        public Exception LastException { get; }
        public List<SignalRCallData> CallStats { get; } = new List<SignalRCallData>();

        public LoadTestThreadResults(int threadID)
        {
            ThreadID = threadID;
            ClientStartTime = DateTime.Now.ToString("H:mm:ss:ffffff");
        }
    }
}
