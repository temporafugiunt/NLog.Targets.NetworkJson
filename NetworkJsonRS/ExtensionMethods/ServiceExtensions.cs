using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace NetworkJsonRS.ExtensionMethods
{
    public static class ServiceExtensions
    {
        public static string GetRealServiceName(this ServiceBase service, string alternativeName)
        {
            // Do some more work to find out our service name, this only works if the process contains a single service.
            // If there are more than one services hosted in the process you will have to do something else.

            var processId = System.Diagnostics.Process.GetCurrentProcess().Id;
            var query = "SELECT * FROM Win32_Service where ProcessId = " + processId;
            var searcher = new System.Management.ManagementObjectSearcher(query);

            foreach (var queryObj in searcher.Get())
            {
                return (queryObj["Name"].ToString());
            }

            return alternativeName;
        }
    }
}
