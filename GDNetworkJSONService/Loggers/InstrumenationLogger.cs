using System.Reflection;
using NLog;
using PV.Logging;

namespace GDNetworkJSONService.Loggers
{
    /// <summary>
    /// Performs the NLog logging for the NetworkJsonRS windows service.
    /// 
    /// To Use this class place the following line at the top of any class you want to log instrumentation messages from. 
    /// 
    /// private InstrumentationLogger _iLogger = LoggerFactory.GetInstrumentationLogger();
    /// 
    /// </summary>
    internal class InstrumentationLogger : BasicInstrumentationLogger
    {
        public InstrumentationLogger(Assembly parentApplication, string applicationLoggingId) : base(parentApplication, applicationLoggingId)
        {
        }

        protected sealed override void SetDerivedClassCustomProperties(LogEventInfo logEventInfo)
        {
            
        }
    }
}
