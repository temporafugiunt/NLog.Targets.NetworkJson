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
        private Lazy<ITransport> _lazyITransport;

        [Required]
        public Uri Endpoint { get; set; }

        [ArrayParameter(typeof(GelfParameterInfo), "parameter")]
        public IList<GelfParameterInfo> Parameters { get; private set; }

        public string Facility { get; set; }

        public IConverter Converter { get; private set; }
        public IEnumerable<ITransport> Transports { get; private set; }
        public DnsBase Dns { get; private set; }

        public GelfTarget() : this(new[]{new UdpTransport(new UdpTransportClient())}, 
            new GelfConverter(), 
            new DnsWrapper())
        {
        }

        public GelfTarget(IEnumerable<ITransport> transports, IConverter converter, DnsBase dns)
        {
            Dns = dns;
            Transports = transports;
            Converter = converter;
            this.Parameters = new List<GelfParameterInfo>();
            _lazyIpEndoint = new Lazy<IPEndPoint>(() =>
            {
                var addresses = Dns.GetHostAddresses(Endpoint.Host);
                var ip = addresses
                    .Where(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    .FirstOrDefault();
                return new IPEndPoint(ip, Endpoint.Port);
            });
            _lazyITransport = new Lazy<ITransport>(() =>
            {
                return Transports.Single(x => x.Scheme.ToUpper() == Endpoint.Scheme.ToUpper());
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
            _lazyITransport.Value
                .Send(_lazyIpEndoint.Value, jsonObject.ToString(Formatting.None, null));
        }
    }
}
