using System.Net;

namespace NLog.Targets.NetworkJSON
{
    public interface ITransportClient
    {
        void Send(byte[] datagram, int bytes, IPEndPoint ipEndPoint);
    }
}
