using System;
using System.Threading.Tasks;

namespace MySuperSocketKestrelCore
{
    public abstract class ChannelBase
    {
        public abstract Task ProcessRequest();

        public abstract void SetSendOption(int maxPacketSize, int maxSendingSize, int maxReTryCount);

        public abstract Task SendAsync(ReadOnlySpan<byte> data);
        public abstract Task SendAsync(ArraySegment<byte> data);

        public Action<AnalyzedPacket> OnPackageReceived;
        public Action OnClosed;

        
    }
}
