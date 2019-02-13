using System;
using System.Buffers;

namespace MySuperSocketCore
{
    public interface IPipelineFilterFactory
    {
        IPipelineFilter Create(object client);
    }


    public class AnalyzedPacket
    {
        public UInt64 SessionUniqueId;
        public UInt16 PacketId;
        public byte[] Head;
        public byte[] Body; 
    }
}