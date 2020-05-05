using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MqttHomeClient.Domain
{
    public class WakeOnLanLogic
    {
        private readonly ILogger<WakeOnLanLogic> _logger;

        public WakeOnLanLogic(ILogger<WakeOnLanLogic> logger)
        {
            _logger = logger;
        }

        public async Task ExecWol(string target)
        {
            //Magic Packetは先頭6バイトが0xFF、以降MACアドレスを16回繰り返す。
            var packet = "FF-FF-FF-FF-FF-FF";
            for (var i = 0; i < 16; i++)
            {
                packet += "-" + target;
            }
            var packetData = packet.Split('-').Select(s => byte.Parse(s, NumberStyles.AllowHexSpecifier)).ToArray();

            try
            {
                using var udpClient = new UdpClient { EnableBroadcast = true };
                await udpClient.SendAsync(packetData, packetData.Length, new IPEndPoint(IPAddress.Parse("255.255.255.255"), 9));
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
            }
        }
    }
}
