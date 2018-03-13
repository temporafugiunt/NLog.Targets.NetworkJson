using System.Reflection;
using NLog;
using PV.Logging;

namespace GDNetworkJSONService.Loggers
{
    internal class DiagnosticsInstrumentationLogger : LoggerBase
    {
        public DiagnosticsInstrumentationLogger(Assembly parentApplication, string applicationLoggingId)
            : base(parentApplication, LogMessageTypes.Instrumentation.ToString(), applicationLoggingId)
        {
            EmptyAllQueuesOnLogWrite = true;
        }

        #region Properties

        public string LoggingDB { get; set; }
        public long DiagnosticsIntervalMS { get; set; }
        public int LogItemsSentFirstTry { get; set; }
        public int LogItemsSentOnRetry { get; set; }
        public int LogItemsFailedFirstTry { get; set; }
        public int LogItemsFailedOnRetry { get; set; }

        public long BacklogCount { get; set; }

        public long DeadLetterCount { get; set; }

        #endregion

        #region Instrumentation Logging Methods

        private const string DiagnosticsMessage = "Logger Diagnostics";

        public void LogFullDiagnostics()
        {
            PushInfo(nameof(LoggingDB), LoggingDB);
            PushInfo(nameof(LogItemsSentFirstTry), LogItemsSentFirstTry.ToString());
            PushInfo(nameof(LogItemsSentOnRetry), LogItemsSentOnRetry.ToString());
            PushInfo(nameof(LogItemsFailedFirstTry), LogItemsFailedFirstTry.ToString());
            PushInfo(nameof(LogItemsFailedOnRetry), LogItemsFailedOnRetry.ToString());
            PushInfo(nameof(BacklogCount), BacklogCount.ToString());
            PushInfo(nameof(DeadLetterCount), DeadLetterCount.ToString());
            PushInfo(nameof(DiagnosticsIntervalMS), DiagnosticsIntervalMS.ToString());

            if (DeadLetterCount > 0)
            {
                LogError(DiagnosticsMessage);
            }
            else if (LogItemsFailedOnRetry > 0)
            {
                LogWarn(DiagnosticsMessage);
            }
            else
            {
                LogInfo(DiagnosticsMessage);
            }
        }
        
        #endregion

        #region Override Methods

        protected sealed override void SetCustomProperties(LogEventInfo logEvent)
        {
            logEvent.Properties["loggingDB"] = LoggingDB;
            logEvent.Properties["logItemsSentFirstTry"] = LogItemsSentFirstTry;
            logEvent.Properties["logItemsSentOnRetry"] = LogItemsSentOnRetry;
            logEvent.Properties["logItemsFailedFirstTry"] = LogItemsFailedFirstTry;
            logEvent.Properties["logItemsFailedOnRetry"] = LogItemsFailedOnRetry;
            logEvent.Properties["deadLetterCount"] = DeadLetterCount;
            logEvent.Properties["backlogCount"] = BacklogCount;
            logEvent.Properties["diagnosticsIntervalMS"] = DiagnosticsIntervalMS;
        }
        
        #endregion
    }
}
