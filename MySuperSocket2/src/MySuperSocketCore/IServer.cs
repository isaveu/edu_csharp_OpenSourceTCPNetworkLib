using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MySuperSocketCore
{
    public interface IServer
    {
        string Name { get; }

        Task<bool> StartAsync();

        Task StopAsync();
    }


    public class ServerBuildParameter
    {
        public Action<AppSession> NetEventOnConnect;
        public Action<AppSession> NetEventOnCloese;
        public Action<AppSession, AnalyzedPacket> NetEventOnReceive;
        public ServerOptions serverOption = new ServerOptions();        
    }
}