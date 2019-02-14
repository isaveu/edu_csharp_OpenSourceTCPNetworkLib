using System;
using System.Buffers;

namespace MySuperSocketKestrelCore
{
    public interface IPipelineFilter
    {
        AnalyzedPacket Filter(ref ReadOnlySequence<byte> buffer);

        IPipelineFilter NextFilter { get; }
    }


    public class AnalyzedPacket
    {
        public UInt64 SessionUniqueId;
        public UInt16 PacketId;
        public byte[] Head;
        public byte[] Body;
    }
}