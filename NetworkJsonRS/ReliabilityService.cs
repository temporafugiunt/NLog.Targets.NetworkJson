using System.Data;
using System.ServiceProcess;
using NetworkJsonRS.ExtensionMethods;
using NetworkJsonRS.Models;
using NLog.Targets.NetworkJSON.Extensions;

namespace NetworkJsonRS
{
    partial class ReliabilityService : ServiceBase
    {
        private CommandLineModel Model { get; }

        public ReliabilityService(CommandLineModel model)
        {
            Model = model;
            ServiceName = "NetworkJSON Reliability Service";
            InitializeComponent();
        }

        public void OnStartConsoleMode()
        {
            OnStart(null);
        }

        public void OnStopConsoleMode()
        {
            OnStop();
        }

        protected override void OnStart(string[] args)
        {
            // TODO: Add code here to start your service.
        }

        protected override void OnStop()
        {
            // TODO: Add code here to perform any tear-down necessary to stop your service.
        }

        private string _realServiceName;
        public string RealServiceName
        {
            get
            {
                if (_realServiceName.IsNullOrEmpty())
                {
                    _realServiceName = this.GetRealServiceName(ServiceName);
                }
                return(_realServiceName);
            }
        }
    }
}
