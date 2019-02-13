using System;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Buffers;


namespace MySuperSocketCore
{
    public class AppSession
    {
        internal AppSession(SuperSocketServer server, ChannelBase channel)
        {
            Server = server;
            Channel = channel;
            SessionID = Guid.NewGuid().ToString();
            UniqueId = server.NextSessionUniqueId();
        }

        public string SessionID { get; private set; }

        public UInt64 UniqueId { get; private set; }

        public SuperSocketServer Server { get; private set; }

        public ChannelBase Channel { get; private set; }        

    }
    
 
}