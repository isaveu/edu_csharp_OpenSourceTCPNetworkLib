using System;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Buffers;


namespace MySuperSocketCore
{
    public class AppSession
    {
        internal AppSession(UInt64 uniqueId, ChannelBase channel)
        {
            //Server = server;
            Channel = channel;
            SessionID = Guid.NewGuid().ToString();
            UniqueId = uniqueId;
        }

        public string SessionID { get; private set; }

        public UInt64 UniqueId { get; private set; }

        //public SuperSocketServer Server { get; private set; }

        public ChannelBase Channel { get; private set; }        

    }
    
 
}