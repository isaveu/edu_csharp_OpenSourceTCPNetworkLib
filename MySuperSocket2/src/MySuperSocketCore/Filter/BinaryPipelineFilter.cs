using System;
using System.Buffers;
using Microsoft.Extensions.Logging;

namespace MySuperSocketCore
{
    public class BinaryPipelineFilter : PipelineFilterBase
    {
        // TotalSize(UInt16) | ProtocolId(UInt16) | Type(SByte) | Padding
        const UInt16 HEADER_SIZE = 5;

        public override AnalyzedPacket Filter(ref ReadOnlySequence<byte> buffer)
        {
            try
            {
                if (buffer.Length < HEADER_SIZE)
               {
                   return null;
               }

               var woringSpan = buffer.First.Span;
                                
                var packetTotalSize = C3SockNetUtil.FastBinaryRead.UInt16(woringSpan, 0);
                var BodySize = (UInt16)(packetTotalSize - HEADER_SIZE);
                var ProtocolId = C3SockNetUtil.FastBinaryRead.UInt16(woringSpan, 2);
                var PacketType = C3SockNetUtil.FastBinaryRead.SByte(woringSpan, 4);

                if (BodySize == 0)
                {
                    var packet = new AnalyzedPacket
                    {
                        SessionUniqueId = 0, 
                        PacketId = ProtocolId,
                        Head = null,
                        Body = buffer.ToArray()
                    };

                    buffer = buffer.Slice(packetTotalSize);
                    return packet;
                }
                else if( BodySize > 0 && (packetTotalSize >= buffer.Length))
                {                    
                    var packet =  new AnalyzedPacket
                    {
                        SessionUniqueId = 0,
                        PacketId = ProtocolId,
                        Head = null,
                        Body = buffer.Slice(HEADER_SIZE, BodySize).ToArray()
                    };

                    buffer = buffer.Slice(packetTotalSize);
                    return packet;
                }
                else
                {
                    return null;
                }                
            }
            catch (Exception ex)
            {
                GLogging.Logger().LogError($"Failed to Filter {ex.ToString()}");
                return null;
            }
        }
                

    }
}
