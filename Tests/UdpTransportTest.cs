using System;
using System.Net;
using Moq;
using NLog;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using NLog.Targets.Gelf;
using NLog.Targets.Gelf.Tests.Resources;

namespace NLog.Targets.Gelf.UnitTest
{
    public class UdpTransportTest
    {
        [TestFixture]
        public class SendMethod
        {
            [Test]
            public void ShouldSendShortUdpMessage()
            {
                var transportClient = new Mock<ITransportClient>();
                var transport = new UdpTransport(transportClient.Object);
                var converter = new Mock<IConverter>();
                var dnslookup = new Mock<DnsBase>();
                converter.Setup(c => c.GetGelfJson(It.IsAny<LogEventInfo>(), It.IsAny<string>())).Returns(new JObject());

                var target = new GelfTarget(new []{transport}, converter.Object, dnslookup.Object) {
                    Endpoint = "udp://127.0.0.1:12201"
                };
                var logEventInfo = new LogEventInfo { Message = "Test Message" };
                dnslookup.Setup(x => x.GetHostAddresses(It.IsAny<string>())).Returns(new[] { IPAddress.Parse("127.0.0.1") });

                target.WriteLogEventInfo(logEventInfo);

                transportClient.Verify(t => t.Send(It.IsAny<byte[]>(), It.IsAny<Int32>(), It.IsAny<IPEndPoint>()), Times.Once());
                converter.Verify(c => c.GetGelfJson(It.IsAny<LogEventInfo>(), It.IsAny<string>()), Times.Once());
            }

            [Test]
            public void ShouldSendLongUdpMessage()
            {
                var jsonObject = new JObject();
                var message = ResourceHelper.GetResource("LongMessage.txt").ReadToEnd();
                
                jsonObject.Add("full_message", JToken.FromObject(message));

                var converter = new Mock<IConverter>();
                converter.Setup(c => c.GetGelfJson(It.IsAny<LogEventInfo>(), It.IsAny<string>())).Returns(jsonObject).Verifiable();
                var transportClient = new Mock<ITransportClient>();
                transportClient.Setup(t => t.Send(It.IsAny<byte[]>(), It.IsAny<Int32>(), It.IsAny<IPEndPoint>())).Verifiable();
               
                var transport = new UdpTransport(transportClient.Object);
                var dnslookup = new Mock<DnsBase>();
                dnslookup.Setup(x => x.GetHostAddresses(It.IsAny<string>())).Returns(new []{IPAddress.Parse("127.0.0.1")});
                var target = new GelfTarget(new[] { transport }, converter.Object, dnslookup.Object) { Endpoint = "udp://127.0.0.1:12201" };
                target.WriteLogEventInfo(new LogEventInfo());

                transportClient.Verify(t => t.Send(It.IsAny<byte[]>(), It.IsAny<Int32>(), It.IsAny<IPEndPoint>()), Times.Exactly(2));
                converter.Verify(c => c.GetGelfJson(It.IsAny<LogEventInfo>(), It.IsAny<string>()), Times.Once());
            }
        }
        
    }
}
