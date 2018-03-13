using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets.NetworkJSON.GuaranteedDelivery;

namespace NLog.Targets.NetworkJSON
{
    [Target("NetworkJSON")]
    public class NetworkJsonTarget : TargetWithLayout, IGDEndpointWriter
    {
        private Lazy<IPEndPoint> _lazyIpEndoint;
        private Lazy<ITransport> _lazyITransport;
        private Uri _endpoint;

        public bool AllowMultiWrite => false;

        #region Task Properties

        [Required]
        public string Endpoint
        {
            get { return _endpoint.ToString(); }
            set { _endpoint = value != null ? new Uri(Environment.ExpandEnvironmentVariables(value)) : null; }
        }

        [ArrayParameter(typeof (ParameterInfo), "parameter")]
        public IList<ParameterInfo> Parameters { get; private set; }

        public bool OnFailStoreAndForward { get; set; }

        #endregion

        private IConverter Converter { get; set; }
        private IEnumerable<ITransport> Transports { get; set; }
        private DnsBase Dns { get; set; }

        public NetworkJsonTarget()
            : this(
                new List<ITransport>()
                {
                    new UdpTransport(new UdpTransportClient()),
                    new TcpTransport(new TcpTransportClient())
                },
                new JsonConverter(),
                new DnsWrapper())
        {
        }

        public NetworkJsonTarget(IEnumerable<ITransport> transports, IConverter converter, DnsBase dns)
        {
            Dns = dns;
            Transports = transports;
            Converter = converter;
            this.Parameters = new List<ParameterInfo>();
            _lazyIpEndoint = new Lazy<IPEndPoint>(() =>
            {
                var addresses = Dns.GetHostAddresses(_endpoint.Host);
                var ip = addresses.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);

                return new IPEndPoint(ip, _endpoint.Port);
            });
            _lazyITransport = new Lazy<ITransport>(() =>
            {
                return Transports.Single(x => x.Scheme.ToUpper() == _endpoint.Scheme.ToUpper());
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

            var jsonObject = Converter.GetLogEventJson(logEvent);
            if (jsonObject == null) return;
            var logEventAsJsonString = jsonObject.ToString(Formatting.None, null);
            Write(logEventAsJsonString);
        }

        /// <summary>
        /// Exposed for the guaranteed delivery service to use.
        /// </summary>
        public void Write(string logEventAsJsonString)
        {
            _lazyITransport.Value.Send(_lazyIpEndoint.Value, logEventAsJsonString);
        }

        public void Write(string[] logEventsAsJsonStrings)
        {
            throw new NotImplementedException($"{nameof(AllowMultiWrite)} is false for {nameof(NetworkJsonTarget)}");
        }
    }
}
