using System;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Buffers;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;

namespace MySuperSocketKestrelCore
{
    public class AppSession<TPackageInfo> : PipeChannel<TPackageInfo>, IAppSession
        where TPackageInfo : class
    {
        public AppSession(TransportConnection transportConnection, IPipelineFilter<TPackageInfo> pipelineFilter)
            : base(transportConnection, pipelineFilter)
        {

        }

        public IServer Server { get; internal set; }
    }
}