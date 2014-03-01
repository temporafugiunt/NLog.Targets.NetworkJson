using System.ComponentModel.DataAnnotations;
using NLog;
using NLog.Targets;
using Newtonsoft.Json;
using NLog.Config;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Net;

namespace NLog.Targets.Gelf
{
    [Target("Gelf")]
    public class GelfTarget : TargetWithLayout
    {
        private Lazy<IPEndPoint> _lazyIpEndoint;
        [Required]
        public string Host { get; set; }

        [Required]
        public int HostPort { get; set; }

        [ArrayParameter(typeof(GelfParameterInfo), "parameter")]
        public IList<GelfParameterInfo> Parameters { get; private set; }

        public string Facility { get; set; }

        public IConverter Converter { get; private set; }
        public ITransport Transport { get; private set; }
        public DnsBase Dns { get; private set; }

        public GelfTarget() : this(new UdpTransport(new UdpTransportClient()), new GelfConverter(), new DnsWrapper())
        {
        }

        public GelfTarget(ITransport transport, IConverter converter, DnsBase dns)
        {
            Dns = dns;
            Transport = transport;
            Converter = converter;
            this.Parameters = new List<GelfParameterInfo>();
            _lazyIpEndoint = new Lazy<IPEndPoint>(() =>
            {
                var addresses = Dns.GetHostAddresses(Host);
                var ip = addresses
                    .Where(x=>x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    .FirstOrDefault();
                return new IPEndPoint(ip, HostPort);
            });
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
            Transport.Send(_lazyIpEndoint.Value, jsonObject.ToString(Formatting.None, null));
        }
    }
}
