using System;
using System.Net;
using NLog.Targets.NetworkJSON;
using NUnit.Framework;

namespace NLog.Targets.NetworkJson.Tests
{
    public class JsonConverterTest
    {
        [TestFixture(Category = "JsonConverter")]
        public class GetJsonMethod
        {
            [Test]
            public void ShouldCreateJsonCorrectly()
            {
                var timestamp = DateTime.Now;
                var logEvent = new LogEventInfo
                                   {
                                       Message = "Test Log Message", 
                                       Level = LogLevel.Info, 
                                       TimeStamp = timestamp,
                                       LoggerName = "JsonConverterTestLogger"
                                   };
                logEvent.Properties.Add("customproperty1", "customvalue1");
                logEvent.Properties.Add("customproperty2", "customvalue2");
                logEvent.Properties.Add("custompropertyint", 199);
                logEvent.Properties.Add("custompropertyarray", new[]{1,2,3});

                var jsonObject = new JsonConverter().GetLogEventJson(logEvent);

                Assert.IsNotNull(jsonObject);
                // Always present properties
                Assert.AreEqual("Test Log Message", jsonObject.Value<string>("message"));
                Assert.AreEqual(timestamp, jsonObject.Value<DateTime>("clientTimestamp"));
                Assert.AreEqual(LogLevel.Info.ToString(), jsonObject.Value<string>("logLevel"));
                Assert.Greater(jsonObject.Value<int>("logSequenceId"), 0);
                
                // Custom properties
                Assert.AreEqual("customvalue1", jsonObject.Value<string>("customproperty1"));
                Assert.AreEqual("customvalue2", jsonObject.Value<string>("customproperty2"));
                Assert.AreEqual(199, jsonObject.Value<int>("custompropertyint"));
                Assert.AreEqual(new[] { 1, 2, 3 }, jsonObject["custompropertyarray"].ToObject<int[]>());
                
                // Make sure we have our 8 base properties (4 required and 4 custom).
                Assert.AreEqual(8, jsonObject.Count);
            }

            [Test]
            public void ShouldHandleExceptionCorrectly()
            {
                var timestamp = DateTime.Now;
                var logEvent = new LogEventInfo
                {
                    Message = "Test Message",
                    Exception = new DivideByZeroException("div by 0"),
                    Level = LogLevel.Error,
                    TimeStamp = timestamp,
                    LoggerName = "JsonConverterTestLogger"
                };

                var jsonObject = new JsonConverter().GetLogEventJson(logEvent);

                Assert.IsNotNull(jsonObject);
                Assert.AreEqual("Test Message", jsonObject.Value<string>("message"));
                Assert.AreEqual(timestamp, jsonObject.Value<DateTime>("clientTimestamp"));
                Assert.AreEqual(LogLevel.Error.ToString(), jsonObject.Value<string>("logLevel"));
                Assert.Greater(jsonObject.Value<int>("logSequenceId"), 0);

                Assert.AreEqual(null, jsonObject.Value<string>("ExceptionSource"));
                Assert.AreEqual("div by 0", jsonObject.Value<string>("ExceptionMessage"));
                Assert.AreEqual(null, jsonObject.Value<string>("StackTrace"));

                // Base properties plus 3 new properties related to exceptions
                Assert.AreEqual(7, jsonObject.Count);
            }

            [Test]
            public void ShouldHandleNestedExceptionCorrectly()
            {
                var timestamp = DateTime.Now;
                var logEvent = new LogEventInfo
                {
                    Message = "Test Message",
                    Exception = new Exception("Outer Exception Detail", new Exception("Inner Exception Detail")),
                    Level = LogLevel.Error,
                    TimeStamp = timestamp,
                    LoggerName = "JsonConverterTestLogger"
                };

                var jsonObject = new JsonConverter().GetLogEventJson(logEvent);

                Assert.IsNotNull(jsonObject);
                Assert.AreEqual("Test Message", jsonObject.Value<string>("message"));
                Assert.AreEqual(timestamp, jsonObject.Value<DateTime>("clientTimestamp"));
                Assert.AreEqual(LogLevel.Error.ToString(), jsonObject.Value<string>("logLevel"));
                Assert.Greater(jsonObject.Value<int>("logSequenceId"), 0);

                Assert.AreEqual(null, jsonObject.Value<string>("ExceptionSource"));
                Assert.AreEqual("Outer Exception Detail - Inner Exception Detail", jsonObject.Value<string>("ExceptionMessage"));
                Assert.AreEqual(null, jsonObject.Value<string>("StackTrace"));

                // Base properties plus 3 new properties related to exceptions
                Assert.AreEqual(7, jsonObject.Count);
            }

            [Test]
            public void ShouldHandle10NestedExceptionCorrectly()
            {
                // It should ignore this 11th nested exception as 10 is the max it will handle.
                var nestedException = new Exception("Inner Exception Detail - 10");
                for (var i = 9; i > 0; i--)
                {
                    var nextException = new Exception("Inner Exception Detail - " + i.ToString(), nestedException);
                    nestedException = nextException;
                }
                var outerException = new Exception("Outer Exception Detail", nestedException);

                var timestamp = DateTime.Now;
                var logEvent = new LogEventInfo
                {
                    Message = "Test Message",
                    Exception = outerException,
                    Level = LogLevel.Error,
                    TimeStamp = timestamp,
                    LoggerName = "JsonConverterTestLogger"
                };

                var jsonObject = new JsonConverter().GetLogEventJson(logEvent);

                Assert.IsNotNull(jsonObject);
                Assert.AreEqual("Test Message", jsonObject.Value<string>("message"));
                Assert.AreEqual(timestamp, jsonObject.Value<DateTime>("clientTimestamp"));
                Assert.AreEqual(LogLevel.Error.ToString(), jsonObject.Value<string>("logLevel"));
                Assert.Greater(jsonObject.Value<int>("logSequenceId"), 0);

                Assert.AreEqual(null, jsonObject.Value<string>("ExceptionSource"));
                const string expectedExceptionDetail =
                    "Outer Exception Detail - Inner Exception Detail - 1 - Inner Exception Detail - 2 - Inner Exception Detail - 3 - Inner Exception Detail - 4 - Inner Exception Detail - 5 - Inner Exception Detail - 6 - Inner Exception Detail - 7 - Inner Exception Detail - 8 - Inner Exception Detail - 9 - Inner Exception Detail - 10";
                Assert.AreEqual(expectedExceptionDetail, jsonObject.Value<string>("ExceptionMessage"));
                Assert.AreEqual(null, jsonObject.Value<string>("StackTrace"));

                // Base properties plus 3 new properties related to exceptions
                Assert.AreEqual(7, jsonObject.Count);
            }

            [Test]
            public void ShouldHandleLongMessageCorrectly()
            {
                var timestamp = DateTime.Now;
                var logEvent = new LogEventInfo
                {
                    //The first 300 chars of lorem ipsum...
                    Message = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Phasellus interdum est in est cursus vitae pellentesque felis lobortis. Donec a orci quis ante viverra eleifend ac et quam. Donec imperdiet libero ut justo tincidunt non tristique mauris gravida. Fusce sapien eros, tincidunt a placerat nullam.",
                    Level = LogLevel.Info,
                    TimeStamp = timestamp,
                    LoggerName = "JsonConverterTestLogger"
                };

                var jsonObject = new JsonConverter().GetLogEventJson(logEvent);

                Assert.IsNotNull(jsonObject);
                Assert.AreEqual(300, jsonObject.Value<string>("message").Length);
                Assert.AreEqual(timestamp, jsonObject.Value<DateTime>("clientTimestamp"));
                Assert.AreEqual(LogLevel.Info.ToString(), jsonObject.Value<string>("logLevel"));
                Assert.Greater(jsonObject.Value<int>("logSequenceId"), 0);
            }
        }
    }
}
