using System;
using System.Buffers;
using System.Text;

namespace MySuperSocketCore
{
    public class LinePipelineFilter : TerminatorPipelineFilter
    {

        public LinePipelineFilter()
            : base(FindPos, TerminatorLen)
        {
        }

        static int TerminatorLen = 2;

        static bool FindPos(ReadOnlySequence<byte> buffer)
        {
            if(buffer.Length <= TerminatorLen)
            {
                return false;
            }

            var woringSpan = buffer.First.Span;
            if (woringSpan[0] == (byte)'\r' && woringSpan[1] == (byte)'\n')
            {
                return true;
            }

            return false;
        }

        public override AnalyzedPacket ResolvePackage(ReadOnlySequence<byte> buffer)
        {
            return new AnalyzedPacket
            {
                SessionUniqueId = 0,
                PacketId = 0,
                Body = buffer.ToArray()
            };
        }
    }
}
