using System;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Buffers;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;

namespace MySuperSocketKestrelCore
{
    public class AppSession
    {
        public AppSession(/*TransportConnection transportConnection,*/ ChannelBase channel)
        {
            Channel = channel;
        }

 
        public ChannelBase Channel { get; private set; }
    }
}