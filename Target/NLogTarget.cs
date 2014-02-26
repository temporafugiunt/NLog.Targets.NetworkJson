using System.ComponentModel.DataAnnotations;
using NLog;
using NLog.Targets;
using Newtonsoft.Json;
using NLog.Config;
using System.Collections.Generic;

namespace Gelf4NLog.Target
{
    [Target("GrayLog")]
    public class NLogTarget : TargetWithLayout
    {
        [Required]
        public string HostIp { get; set; }

        [Required]
        public int HostPort { get; set; }

        [ArrayParameter(typeof(GelfParameterInfo), "parameter")]
        public IList<GelfParameterInfo> Parameters { get; private set; }

        public string Facility { get; set; }

        public IConverter Converter { get; private set; }
        public ITransport Transport { get; private set; }

        public NLogTarget() : this(new UdpTransport(new UdpTransportClient()), new GelfConverter())
        {
        }

        public NLogTarget(ITransport transport, IConverter converter)
        {
            Transport = transport;
            Converter = converter;
            this.Parameters = new List<GelfParameterInfo>();
        }

        public void WriteLogEventInfo(LogEventInfo logEvent)
        {
            Write(logEvent);
        }

        protected override void Write(LogEventInfo logEvent)
        {
            foreach (var par in this.Parameters)
            {
                if (!logEvent.Properties.ContainsKey(par.Name))
                {
                    string stringValue = par.Layout.Render(logEvent);

                    logEvent.Properties.Add(par.Name, stringValue);
                }
            }

            var jsonObject = Converter.GetGelfJson(logEvent, Facility);
            if (jsonObject == null) return;
            Transport.Send(HostIp, HostPort, jsonObject.ToString(Formatting.None, null));
        }
    }
}
