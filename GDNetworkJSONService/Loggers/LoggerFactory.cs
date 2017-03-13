using System.Reflection;
using PV.Logging;

namespace GDNetworkJSONService.Loggers
{
    internal class LoggerFactory
    {
        private const string ApplicationLoggingIdNonGD = "pv-supportapp-gdservicenetworkjsonnogd";
        private const string ApplicationLoggingId = "pv-supportapp-gdservicenetworkjson";

        public static DiagnosticsInstrumentationLogger GetDiagnosticsInstrumentationLogger(bool useGuaranteedDelivery = true)
        {
            if (useGuaranteedDelivery)
            {
                return new DiagnosticsInstrumentationLogger(Assembly.GetExecutingAssembly(), ApplicationLoggingId);
            }
            return new DiagnosticsInstrumentationLogger(Assembly.GetExecutingAssembly(), ApplicationLoggingIdNonGD);
        }

        public static InstrumentationLogger GetInstrumentationLogger(bool useGuaranteedDelivery = true)
        {
            useGuaranteedDelivery = true;
            if (useGuaranteedDelivery)
            {
                return new InstrumentationLogger(Assembly.GetExecutingAssembly(), ApplicationLoggingId);
            }
            return new InstrumentationLogger(Assembly.GetExecutingAssembly(), ApplicationLoggingIdNonGD);
        }

        public static MessageLogger GetMessageLogger(bool useGuaranteedDelivery = true)
        {
            useGuaranteedDelivery = true;
            if (useGuaranteedDelivery)
            {
                return new MessageLogger(Assembly.GetExecutingAssembly(), LogMessageTypes.LogMessage.ToString(), ApplicationLoggingId);
            }
            return new MessageLogger(Assembly.GetExecutingAssembly(), LogMessageTypes.LogMessage.ToString(), ApplicationLoggingIdNonGD);
        }

        public static BasicConfigLogger GetConfigLogger(bool useGuaranteedDelivery = true)
        {
            if (useGuaranteedDelivery)
            {
                return new BasicConfigLogger(Assembly.GetExecutingAssembly(), ApplicationLoggingId);
            }
            return new BasicConfigLogger(Assembly.GetExecutingAssembly(), ApplicationLoggingIdNonGD);
        }
    }
}
