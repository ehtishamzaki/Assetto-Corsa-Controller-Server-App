using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ACController.Services
{
    public class OscService : IDisposable
    {
        private readonly UdpClient _client;
        private readonly IPEndPoint _endpoint;
        private int _currentScene = -1;
        private const int DefaultPort = 9000;

        public OscService(int port = DefaultPort)
        {
            _endpoint = new IPEndPoint(IPAddress.Loopback, port);
            _client = new UdpClient();
        }

        public void SendScene(int scene)
        {
            if (scene == _currentScene)
                return;

            _currentScene = scene;

            try
            {
                byte[] oscMessage = BuildOscMessage($"/scene/{scene}");
                _client.Send(oscMessage, oscMessage.Length, _endpoint);
            }
            catch
            {
            }
        }

        private byte[] BuildOscMessage(string address)
        {
            int addressLen = address.Length;
            int paddedAddressLen = (addressLen + 4) & ~3;

            byte[] message = new byte[paddedAddressLen + 4];

            Encoding.ASCII.GetBytes(address, 0, addressLen, message, 0);

            message[paddedAddressLen] = (byte)',';

            return message;
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}