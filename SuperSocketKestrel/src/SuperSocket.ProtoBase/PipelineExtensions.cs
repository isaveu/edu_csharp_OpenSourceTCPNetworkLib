using System;
using System.Buffers;


namespace SuperSocket.ProtoBase
{
    public static class PipelineExtensions
    {
        public static bool TrySliceTo(this ReadOnlySequence<byte> buffer, Func<ReadOnlySequence<byte>, bool> checkFunc, out ReadOnlySequence<byte> slice, out SequencePosition position)
        {         
            for(int i = 0; i < buffer.Length; ++i)
            {
                var woring = buffer.Slice(i, (buffer.Length - i));
                if (checkFunc(woring))
                {
                    slice = buffer.Slice(0, i);
                    position = buffer.GetPosition(i);
                    return true;
                }
            }

            slice = buffer;
            position = buffer.GetPosition(0);
            return false;
        }

        
    }
}