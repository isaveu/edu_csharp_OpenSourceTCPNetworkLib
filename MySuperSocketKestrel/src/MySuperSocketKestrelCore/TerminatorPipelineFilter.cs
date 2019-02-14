using System;
using System.Buffers;
using System.Collections;
using System.Threading.Tasks;

namespace MySuperSocketKestrelCore
{
    public abstract class TerminatorPipelineFilter : PipelineFilterBase
    {        
        int TerminatorLen = 0;
        private Func<ReadOnlySequence<byte>, bool > CheckFunc;

        public TerminatorPipelineFilter(Func<ReadOnlySequence<byte>, bool> checkFunc, int terminatorLen)
        {
            CheckFunc = checkFunc;
            TerminatorLen = terminatorLen;
        }

        public override AnalyzedPacket Filter(ref ReadOnlySequence<byte> buffer)
        {
            try
            {
                ReadOnlySequence<byte> slice;
                SequencePosition cursor;

                if (!buffer.TrySliceTo(CheckFunc, out slice, out cursor))
                {
                    return null;
                }

                buffer = buffer.Slice(cursor).Slice(TerminatorLen);
                return ResolvePackage(slice);
            }
            catch(Exception ex)
            {
                //TODO ex 로그 남기기
                return null;
            }
        }

        public abstract AnalyzedPacket ResolvePackage(ReadOnlySequence<byte> buffer);
    }
}