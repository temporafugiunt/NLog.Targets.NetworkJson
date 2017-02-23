using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NLog;
using PV.Logging;

namespace NetworkJsonRS.Loggers
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
