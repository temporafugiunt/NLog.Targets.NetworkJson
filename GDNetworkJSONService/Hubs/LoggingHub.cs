using System;
using System.Threading;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace GDNetworkJSONService.Hubs
{
    [HubName("GDServiceLogger")]
    public class LoggingHub : Hub
    {
        public static string DbConnectionString;

        public static int TotalMessageCount;
        public void StoreAndForward(string forwardEndpoint, string jsonMessage)
        {
            Interlocked.Increment(ref TotalMessageCount);
            Console.WriteLine($"IN={TotalMessageCount}");
            LocalLogStorageDB.LogStorageTable.InsertLogRecord(forwardEndpoint, jsonMessage);
        }
    }
}
