using NLog;
using Newtonsoft.Json.Linq;

namespace NLog.Targets.Gelf
{
    public interface IConverter
    {
        JObject GetGelfJson(LogEventInfo logEventInfo, string facility);
    }
}