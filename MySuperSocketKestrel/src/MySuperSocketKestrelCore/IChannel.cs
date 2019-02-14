using System;
using System.Threading.Tasks;

namespace MySuperSocketKestrelCore
{
    public interface IChannel
    {
        Task ProcessRequest();

        Task SendAsync(ReadOnlySpan<byte> data);

        event EventHandler Closed;

        event Action<IChannel, AnalyzedPacket> PackageReceived;
    }

}
