using System.Net;
namespace NLog.Targets.Gelf
{
    public interface ITransport
    {
        void Send(IPEndPoint target, string message);
    }
}
