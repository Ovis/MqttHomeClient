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


        public async Task<Result> ActionAsync(string text)
        {
            try
            {
                await ExecWol(text);

                return new Result();
            }
            catch (Exception e)
            {
                return new Result
                {
                    Status = ResultStatus.ErrorOnSystem,
                    Message = e.Message,
                    StackTrace = e.StackTrace
                };
            }
        }

        public async Task<bool> ExecWol(string target)
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

            return true;
        }

        public bool QuitAction()
        {
            return true;
        }
    }
}
