using System;
using System.Buffers;
using System.Text;

namespace MySuperSocketKestrelCore
{
    public class LinePipelineFilter : TerminatorPipelineFilter<LinePackageInfo>
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

        public override LinePackageInfo ResolvePackage(ReadOnlySequence<byte> buffer)
        {
            return new LinePackageInfo { Line = buffer.ToString() };
        }
    }
}
