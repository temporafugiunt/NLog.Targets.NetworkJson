using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NLog.Targets.NetworkJSON
{
    /// <summary>
    /// This TCP Transport Client was designed to be sticky and keep a connection to a GrayLog2 server open with KeepAlive enabled unless the IPEndPoint information passed
    /// does not match the data passed the last time a TCP connection was established. In that case it will close the existing session and create a new connection.
    /// </summary>
    public class TcpTransportClient : ITransportClient
    {
        private IPEndPoint _lastIpEndpoint = null;
        private TcpClient _tcpClient = null;
        private NetworkStream _tcpClientStream = null;
        private byte[] _newline = Encoding.UTF8.GetBytes("\n");


        public void Send(byte[] data, int bytes, IPEndPoint ipEndPoint)
        {
            CheckConnection(ipEndPoint);
            var retryOnceOnFailure = false;
            try
            {
                _tcpClientStream.Write(data, 0, bytes);
                _tcpClientStream.Write(_newline, 0, _newline.Length);
            }
            catch (Exception)
            {
                CloseConnection();
                CheckConnection(ipEndPoint);
                retryOnceOnFailure = true;
            }
            if (retryOnceOnFailure)
            {
                _tcpClientStream.Write(data, 0, bytes);
                _tcpClientStream.Write(_newline, 0, _newline.Length);
            }
            
        }

        private void CheckConnection(IPEndPoint ipEndPoint)
        {
            if (_lastIpEndpoint != null)
            {
                // Did we change over to a different connection?
                if (!_lastIpEndpoint.Address.Equals(ipEndPoint.Address) || _lastIpEndpoint.Port != ipEndPoint.Port || _lastIpEndpoint.AddressFamily != ipEndPoint.AddressFamily)
                {
                    CloseConnection();
                }
            }
            
            if (_tcpClient == null || !_tcpClient.Connected)
            {
                ConnectClient(ipEndPoint);
            }
        }

        private void ConnectClient(IPEndPoint ipEndPoint)
        {
            _tcpClient = new TcpClient();
            _tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            _tcpClient.Connect(ipEndPoint);
            _lastIpEndpoint = ipEndPoint;

            _tcpClientStream = _tcpClient.GetStream();
        }

        private void CloseConnection()
        {
            _tcpClient?.Close();
            _tcpClient = null;
        }
    }
}
