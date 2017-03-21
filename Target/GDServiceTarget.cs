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
using Microsoft.AspNet.SignalR.Client;
using NLog.Common;

namespace NLog.Targets.NetworkJSON
{
    [Target("GDService")]
    public class GDServiceTarget : TargetWithLayout
    {
        #region NetworkJson Reliability Service Variables

        private HubConnection _localHubConnection;
        private IHubProxy _localHubProxy;
        private Uri _guaranteedDeliveryEndpoint;
        private Uri _networkJsonEndpoint;

        #endregion

        #region Task Properties

        [Required]
        public string GuaranteedDeliveryEndpoint
        {
            get { return _guaranteedDeliveryEndpoint.ToString(); }
            set
            {
                if (value != null)
                {
                    _guaranteedDeliveryEndpoint = new Uri(Environment.ExpandEnvironmentVariables(value));
                    ClearHubConnection();
                }
                else
                {
                    _guaranteedDeliveryEndpoint = null;
                    ClearHubConnection();
                }
            }
        }

        [Required]
        public string NetworkJsonEndpoint
        {
            get { return _networkJsonEndpoint.ToString(); }
            set
            {
                if (value != null)
                {
                    _networkJsonEndpoint = new Uri(Environment.ExpandEnvironmentVariables(value));
                }
                else
                {
                    _networkJsonEndpoint = null;
                }
            }
        }

        private void ClearHubConnection()
        {
            if (_localHubConnection != null)
            {
                _localHubConnection.Stop();
                _localHubConnection.Dispose();
                _localHubConnection = null;
                _localHubProxy = null;
            }
        }

        private void InitHubConnection()
        {
            _localHubConnection = new HubConnection(GuaranteedDeliveryEndpoint);
            _localHubProxy = _localHubConnection.CreateHubProxy("GDServiceLogger");
            try
            {
                var task = _localHubConnection.Start();
                task.GetAwaiter().GetResult();
            }
            catch (System.Exception)
            {
                try
                {
                    _localHubConnection.Stop();
                    _localHubConnection.Dispose();
                }
                catch (Exception)
                {
                    
                }
                _localHubConnection = null;
                _localHubProxy = null;
            }
            
        }

        [ArrayParameter(typeof(ParameterInfo), "parameter")]
        public IList<ParameterInfo> Parameters { get; }
        
        #endregion

        private IConverter Converter { get; }

        public GDServiceTarget() : this(new JsonConverter())
        {
        }

        public GDServiceTarget(IConverter converter)
        {
            Converter = converter;
            this.Parameters = new List<ParameterInfo>();
        }
        
        public void WriteLogEventInfo(LogEventInfo logEvent)
        {
            Write(logEvent);
        }

        protected override void Write(LogEventInfo logEvent)
        {
            foreach (var par in Parameters)
            {
                if (!logEvent.Properties.ContainsKey(par.Name))
                {
                    string stringValue = par.Layout.Render(logEvent);

                    logEvent.Properties.Add(par.Name, stringValue);
                }
            }
            
            var jsonObject = Converter.GetLogEventJson(logEvent);
            if (jsonObject == null) return;
            var jsonObjectStr = jsonObject.ToString(Formatting.None, null);

            var task = WriteAsync(jsonObjectStr);
            task.GetAwaiter().GetResult();
        }

        /// <summary>
        /// Exposed for unit testing and load testing purposes.
        /// </summary>
        public Task WriteAsync(string logEventAsJsonString)
        {
            if (_localHubConnection == null)
            {
                InitHubConnection();
            }
            if(_localHubConnection == null || _localHubConnection.State != ConnectionState.Connected)
            {
                return Task.FromException(new Exception($"Connection to {_guaranteedDeliveryEndpoint} not online"));
            }
            else
            {
                return _localHubProxy.Invoke("storeAndForward", NetworkJsonEndpoint, logEventAsJsonString);
            }
        }
    }
}
