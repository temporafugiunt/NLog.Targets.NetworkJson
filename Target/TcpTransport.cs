using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;

namespace NLog.Targets.NetworkJSON
{
    public class TcpTransport : ITransport
    {
        private readonly ITransportClient _transportClient;
        public TcpTransport(ITransportClient transportClient)
        {
            _transportClient = transportClient;
        }

        /// <summary>
        /// Sends a TCP Message to GrayLog2 server
        /// </summary>
        /// <param name="serverIpAddress">IP address of the target GrayLog2 server</param>
        /// <param name="serverPort">Port number of the target GrayLog2 instance</param>
        /// <param name="message">Message (in JSON) to log</param>
        public void Send(string serverIpAddress, int serverPort, string message)
        {
            var ipAddress = IPAddress.Parse(serverIpAddress);
            var ipEndPoint = new IPEndPoint(ipAddress, serverPort);

           Send(ipEndPoint, message);
        }

        /// <summary>
        /// Sends a TCP Message to GrayLog2 server
        /// </summary>
        /// <param name="target">IP Endpoint of the  of the target GrayLog2 server</param>
        /// <param name="message">Message (in JSON) to log</param>
        public void Send(IPEndPoint target, string message)
        {
            var ipEndPoint = target;

            var messageAsBytes = Encoding.UTF8.GetBytes(message.Replace("\n", ""));

            _transportClient.Send(messageAsBytes, messageAsBytes.Length, ipEndPoint);
        }
        
        /// <summary>
        /// Compresses the given message using GZip algorithm
        /// </summary>
        /// <param name="message">Message to be compressed</param>
        /// <returns>Compressed message in bytes</returns>
        private static byte[] CompressMessage(String message)
        {
            var compressedMessageStream = new MemoryStream();
            using (var gzipStream = new GZipStream(compressedMessageStream, CompressionMode.Compress))
            {
                var messageBytes = Encoding.UTF8.GetBytes(message);
                gzipStream.Write(messageBytes, 0, messageBytes.Length);
            }

            return compressedMessageStream.ToArray();
        }
        
        public string Scheme
        {
            get { return "tcp"; }
        }
    }
}
