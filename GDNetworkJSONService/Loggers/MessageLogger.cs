using System.Reflection;
using NLog;
using PV.Logging;

namespace GDNetworkJSONService.Loggers
{
    internal class MessageLogger : LoggerBase
    {
        public MessageLogger(Assembly parentApplication, string messageType, string applicationLoggingId) : base(parentApplication, messageType, applicationLoggingId)
        {
        }
        
        protected override void SetCustomProperties(LogEventInfo logEventInfo)
        {
            
        }
    }
}
