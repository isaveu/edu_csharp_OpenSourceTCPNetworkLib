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
        public void SetChannel(ChannelBase channel)
        {
            Channel = channel;
        }

        public void SetUniqueId(UInt64 number, string text)
        {
            UniqueId = number;
            SessionID = text;
        }

        public string SessionID { get; private set; }

        public UInt64 UniqueId { get; private set; }

        public ChannelBase Channel { get; private set; }
    }
}