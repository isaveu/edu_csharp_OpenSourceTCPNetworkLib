using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using MySuperSocketKestrelCore;

namespace TestApp
{
    class Program
    {
        static Microsoft.Extensions.Logging.ILogger Logger;

        static void Main(string[] args)
        {
            RunAsync().Wait();
        }

        static void NetEventOnConnect(AppSession session)
        {
            Logger.LogInformation($"[NetEventOnConnect] A new session connected: {session.SessionID}");
        }

        static void NetEventOnCloese(AppSession session)
        {
            Logger.LogInformation($"[NetEventOnCloese] session DisConnected: {session.SessionID}");
        }

        static void NetEventOnReceive(AppSession session, AnalyzedPacket packet)
        {
            packet.SessionUniqueId = session.UniqueId;
            Logger.LogInformation($"[NetEventOnReceive] session: {session.SessionID}, ReceiveDataSize:{packet.Body.Length}");

            var pktID = packet.PacketId + 1;
            var packetLen = packet.Body.Length + 5;
            var dataSource = new byte[packetLen];
            Buffer.BlockCopy(BitConverter.GetBytes(packetLen), 0, dataSource, 0, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(pktID), 0, dataSource, 2, 2);
            dataSource[4] = 0;
            Buffer.BlockCopy(packet.Body, 0, dataSource, 5, packet.Body.Length);
            session.Channel.SendAsync(packet.Body.AsSpan());
        }

        static async Task RunAsync()
        {
            var parameter = BuildOption();

            var pipelineFilterList = new List<IPipelineFilter>();
            pipelineFilterList.Add(new BinaryPipelineFilter());

            var server = new SuperSocketServer();
            server.CreateSocketServer(parameter, pipelineFilterList);

            Logger = SuperSocketServer.GetLogger();

            await server.StartAsync();

            Logger.LogInformation("The server is started.");
            while (Console.ReadLine().ToLower() != "c")
            {
                continue;
            }

            await server.StopAsync();
        }

        static ServerBuildParameter BuildOption()
        {
            var parameter = new ServerBuildParameter();
            parameter.NetEventOnConnect = NetEventOnConnect;
            parameter.NetEventOnCloese = NetEventOnCloese;
            parameter.NetEventOnReceive = NetEventOnReceive;
            parameter.serverOption.Name = "TestServer";
            parameter.serverOption.Listeners = new ListenOptions[1];
            parameter.serverOption.Listeners[0] = new ListenOptions();
            parameter.serverOption.Listeners[0].Ip = "Any";
            parameter.serverOption.Listeners[0].Port = 11021;
            parameter.serverOption.Listeners[0].BackLog = 100;
            parameter.serverOption.Listeners[0].MaxRecvPacketSize = 512; //TODO 적용 안됨
            parameter.serverOption.Listeners[0].MaxReceivBufferSize = 512 * 3;//TODO 적용 안됨
            parameter.serverOption.Listeners[0].MaxSendPacketSize = 1024;
            parameter.serverOption.Listeners[0].MaxSendingSize = 4096;
            parameter.serverOption.Listeners[0].MaxSendReTryCount = 3;

            return parameter;
        }

    }
}
