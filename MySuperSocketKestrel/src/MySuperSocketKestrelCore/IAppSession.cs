using System;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Buffers;

namespace MySuperSocketKestrelCore
{
    public interface IAppSession : IChannel
    {
        IServer Server { get; }
    }
}