using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace NLog.Targets.NetworkJSON
{
    public class UdpTransport : ITransport
    {
        private readonly ITransportClient _transportClient;
        public UdpTransport(ITransportClient transportClient)
        {
            _transportClient = transportClient;
        }

        /// <summary>
        /// Sends a UDP datagram to LogStash server
        /// </summary>
        /// <param name="serverIpAddress">IP address of the target LogStash server</param>
        /// <param name="serverPort">Port number of the target LogStash instance</param>
        /// <param name="message">Message (in JSON) to log</param>
        public void Send(string serverIpAddress, int serverPort, string message)
        {
            var ipAddress = IPAddress.Parse(serverIpAddress);
            var ipEndPoint = new IPEndPoint(ipAddress, serverPort);

           Send(ipEndPoint, message);
        }

        /// <summary>
        /// Sends a UDP datagram to a LogStash server
        /// </summary>
        /// <param name="target">IP Endpoint of the  of the target LogStash server</param>
        /// <param name="message">Message (in JSON) to log</param>
        public void Send(IPEndPoint target, string message)
        {
            var ipEndPoint = target;

            var compressedMessage = Encoding.UTF8.GetBytes(message);

            _transportClient.Send(compressedMessage, compressedMessage.Length, ipEndPoint);
        }
        
        public string Scheme
        {
            get { return "udp"; }
        }
    }
}
