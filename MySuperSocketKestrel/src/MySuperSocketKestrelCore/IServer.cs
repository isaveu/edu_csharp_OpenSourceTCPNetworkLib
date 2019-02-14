using System;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Buffers;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;

namespace MySuperSocketKestrelCore
{
    public interface IServer
    {
        string Name { get; }

        Task<bool> StartAsync();

        Task StopAsync();
    }
}