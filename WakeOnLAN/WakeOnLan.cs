using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using PluginInterface;

namespace WakeOnLAN
{
    public class WakeOnLan : IPlugin
    {
        public string Topic { get; } = "WakeOnLAN";

        public string PluginName { get; } = "WakeOnLAN";

        public bool Action(string text)
        {
            return false;
        }

        public async Task<bool> ActionAsync(string text)
        {
            return await ExecWol(text);
        }

        public async Task<bool> ExecWol(string target)
        {
            try
            {
                //Magic Packetは先頭6バイトが0xFF、以降MACアドレスを16回繰り返す。
                var packet = "FF-FF-FF-FF-FF-FF";
                for (var i = 0; i < 16; i++)
                {
                    packet += "-" + target;
                }
                var packetData = packet.Split('-').Select(s => byte.Parse(s, NumberStyles.AllowHexSpecifier)).ToArray();

                using var udpClient = new UdpClient { EnableBroadcast = true };
                await udpClient.SendAsync(packetData, packetData.Length, new IPEndPoint(IPAddress.Parse("255.255.255.255"), 9));
            }
            catch (Exception e)
            {
                return false;
            }

            return true;
        }
    }
}
