using System;
using System.Threading.Tasks;

namespace MySuperSocketCore
{
    public abstract class ChannelBase
    {
        public abstract Task ProcessRequest();


        public abstract Task<int> SendAsync(ReadOnlyMemory<byte> buffer);
        public abstract Task<int> SendAsync(ArraySegment<byte> buffer);

        public abstract void SendTask(ReadOnlyMemory<byte> buffer);
        public abstract void SendTask(ArraySegment<byte> buffer);


        public Action<AnalyzedPacket> OnPackageReceived;

        public abstract void SetSendLimit(int maxPacketSize, int maxSendingSize, int maxReTryCount);
    }
}
