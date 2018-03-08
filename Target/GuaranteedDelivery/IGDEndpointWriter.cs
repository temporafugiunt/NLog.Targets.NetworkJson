using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLog.Targets.NetworkJSON.GuaranteedDelivery
{
    /// <summary>
    /// The interface defined for any service which wants to be a Guaranteed Delivery Endpoint Writer.
    /// </summary>
    public interface IGDEndpointWriter
    {
        void Write(string logEventAsJsonString);
    }
}
