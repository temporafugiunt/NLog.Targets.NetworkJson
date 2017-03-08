using System.Reflection;
using PV.Logging;

namespace GDNetworkJSONService.Loggers
{
    internal class LoggerFactory
    {
        private const string ApplicationLoggingId = "pv-supportapp-networkjsonrs";

        public static InstrumentationLogger GetInstrumentationLogger()
        {
            return new InstrumentationLogger(Assembly.GetExecutingAssembly(), ApplicationLoggingId);
        }

        public static MessageLogger GetMessageLogger()
        {
            return new MessageLogger(Assembly.GetExecutingAssembly(), LogMessageTypes.LogMessage.ToString(), ApplicationLoggingId);
        }

        public static BasicConfigLogger GetConfigLogger()
        {
            return new BasicConfigLogger(Assembly.GetExecutingAssembly(), ApplicationLoggingId);
        }
    }
}
