using System.Net;

namespace NLog.Targets.NetworkJSON
{
    public interface ITransport
    {
        string Scheme { get; }
        void Send(IPEndPoint target, string message);
    }
}
