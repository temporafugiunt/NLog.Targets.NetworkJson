using Newtonsoft.Json.Linq;

namespace NLog.Targets.NetworkJSON
{
    public interface IConverter
    {
        JObject GetLogEventJson(LogEventInfo logEventInfo);
    }
}