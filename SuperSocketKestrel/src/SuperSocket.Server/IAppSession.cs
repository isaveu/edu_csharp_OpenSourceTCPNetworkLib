using System;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Buffers;
using SuperSocket.Channel;

namespace SuperSocket.Server
{
    public interface IAppSession : IChannel
    {
        IServer Server { get; }
    }
}