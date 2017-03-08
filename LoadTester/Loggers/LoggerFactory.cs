using System.Reflection;
using PV.Logging;

namespace NLog.Targets.NetworkJSON.LoadTester.Loggers
{
    public class LoggerFactory
    {
        private const string ApplicationLoggingId = "pv-testapp-loadtester";

        public static BasicInstrumentationLogger GetBasicInstrumentationLogger()
        {
            return new BasicInstrumentationLogger(Assembly.GetExecutingAssembly(), ApplicationLoggingId);
        }
    }
}
