using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Newtonsoft.Json.Linq;

namespace NLog.Targets.NetworkJSON
{
    public class JsonConverter : IConverter
    {
        public JObject GetLogEventJson(LogEventInfo logEventInfo)
        {
            //Retrieve the formatted message from LogEventInfo
            var logEventMessage = logEventInfo.FormattedMessage;
            if (logEventMessage == null) logEventMessage = "";


            //Construct the instance of BaseJsonMessage, which is any property that should be in all messages no matter what.
            var jsonMessage = new BaseJsonMessage
            {
                LogLevel = logEventInfo.Level.ToString(),
                Message = logEventMessage,
                LogSequenceId = logEventInfo.SequenceID,
                MessageType = logEventInfo.LoggerName,
                ClientTimestamp = logEventInfo.TimeStamp
            };

            //Convert to JObject
            var jsonObject = JObject.FromObject(jsonMessage);
            
            // If we have stack frame info, add File and Line properties
            if (logEventInfo.UserStackFrame != null)
            {
                logEventInfo.Properties.Add("Line", logEventInfo.UserStackFrame.GetFileLineNumber().ToString(CultureInfo.InvariantCulture));
                logEventInfo.Properties.Add("File", logEventInfo.UserStackFrame.GetFileName());
            }

            // If we are dealing with an exception, add exception properties to LogEventInfo properties
            if (logEventInfo.Exception != null)
            {
                string exceptionDetail;
                string stackDetail;

                GetExceptionMessages(logEventInfo.Exception, out exceptionDetail, out stackDetail);

                logEventInfo.Properties.Add("ExceptionSource", logEventInfo.Exception.Source);
                logEventInfo.Properties.Add("ExceptionMessage", exceptionDetail);
                logEventInfo.Properties.Add("StackTrace", stackDetail);
            }

            // Add all properties to the JObject
            foreach (var property in logEventInfo.Properties)
            {
                AddAdditionalField(jsonObject, property);
            }

            return jsonObject;
        }

        private static void AddAdditionalField(IDictionary<string, JToken> jObject, KeyValuePair<object, object> property)
        {
            var key = property.Key as string;
            if (key == null) return;

            JToken value = null;
            if(property.Value != null)
                value = JToken.FromObject(property.Value);

            jObject.Add(key, value);
        }
        
        /// <summary>
        /// Get the message details from all nested exceptions, up to 10 in depth.
        /// </summary>
        /// <param name="ex">Exception to get details for</param>
        /// <param name="exceptionDetail">Exception message</param>
        /// <param name="stackDetail">Stacktrace with inner exceptions</param>
        private void GetExceptionMessages(Exception ex, out string exceptionDetail, out string stackDetail)
        {
            var exceptionSb = new StringBuilder();
            var stackSb = new StringBuilder();
            var nestedException = ex;
            stackDetail = null;

            int counter = 0;
            do
            {
                exceptionSb.Append(nestedException.Message + " - ");
                if (nestedException.StackTrace != null)
                    stackSb.Append(nestedException.StackTrace + "--- Inner exception stack trace ---");
                nestedException = nestedException.InnerException;
                counter++;
            }
            while (nestedException != null && counter < 11);

            exceptionDetail = exceptionSb.ToString().Substring(0, exceptionSb.Length - 3);
            if (stackSb.Length > 0)
                stackDetail = stackSb.ToString().Substring(0, stackSb.Length - 35);
        }
    }
}
