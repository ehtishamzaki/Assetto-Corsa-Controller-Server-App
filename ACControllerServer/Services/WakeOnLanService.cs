using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ACControllerServer.Services
{
    public class WakeOnLanService
    {
        private readonly ILogger<WakeOnLanService> _logger;

        public WakeOnLanService(ILogger<WakeOnLanService> logger)
        {
            _logger = logger;
        }

        public async Task<int> WakeDevices(List<string> macAddresses)
        {
            int successCount = 0;

            foreach (var mac in macAddresses)
            {
                try
                {
                    await SendMagicPacket(mac);
                    _logger.LogInformation("Wake-on-LAN packet sent to {MacAddress}", mac);
                    successCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send Wake-on-LAN packet to {MacAddress}", mac);
                }
            }

            return successCount;
        }

        private async Task SendMagicPacket(string macAddress)
        {
            byte[] macBytes = ParseMacAddress(macAddress);
            byte[] magicPacket = BuildMagicPacket(macBytes);

            using var client = new UdpClient();
            client.EnableBroadcast = true;

            await client.SendAsync(magicPacket, magicPacket.Length, new IPEndPoint(IPAddress.Broadcast, 9));
            await client.SendAsync(magicPacket, magicPacket.Length, new IPEndPoint(IPAddress.Broadcast, 7));
        }

        private byte[] ParseMacAddress(string macAddress)
        {
            string cleanMac = Regex.Replace(macAddress, "[^0-9A-Fa-f]", "");

            if (cleanMac.Length != 12)
                throw new ArgumentException($"Invalid MAC address format: {macAddress}");

            byte[] bytes = new byte[6];
            for (int i = 0; i < 6; i++)
            {
                bytes[i] = Convert.ToByte(cleanMac.Substring(i * 2, 2), 16);
            }

            return bytes;
        }

        private byte[] BuildMagicPacket(byte[] macBytes)
        {
            byte[] packet = new byte[102];

            for (int i = 0; i < 6; i++)
                packet[i] = 0xFF;

            for (int i = 0; i < 16; i++)
                Array.Copy(macBytes, 0, packet, 6 + (i * 6), 6);

            return packet;
        }
    }
}