using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TServer
{
    public class UdpBroadcaster : IDisposable
    {
        private readonly UdpClient _udpClient;
        private readonly IPEndPoint _multicastEndpoint;

        public UdpBroadcaster(string multicastAddress, int port)
        {
            _udpClient = new UdpClient();
            _udpClient.Client.SendBufferSize = 1024 * 1024; // 1 Мб
            _multicastEndpoint = new IPEndPoint(IPAddress.Parse(multicastAddress), port);
        }

        public async Task SendAsync(long id, double value)
        {
            // Серіалізація: 8 байт для id + 8 байт для value (double)
            byte[] data = new byte[16];
            BitConverter.GetBytes(id).CopyTo(data, 0);
            BitConverter.GetBytes(value).CopyTo(data, 8);

            await _udpClient.SendAsync(data, data.Length, _multicastEndpoint);
        }

        public void Dispose()
        {
            _udpClient?.Dispose();
        }
    }
}
