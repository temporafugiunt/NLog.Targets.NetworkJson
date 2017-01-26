using System;
using Newtonsoft.Json;

namespace NLog.Targets.NetworkJSON
{
    [JsonObject(MemberSerialization.OptIn)]
    public class BaseJsonMessage
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("logSequenceId")]
        public int LogSequenceId { get; set; }

        [JsonProperty("logLevel")]
        public string LogLevel { get; set; }
        
        [JsonProperty("clientTimestamp")]
        public DateTime ClientTimestamp { get; set; }
    }
}
