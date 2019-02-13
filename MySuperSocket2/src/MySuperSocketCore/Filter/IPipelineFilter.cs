using System;
using System.Buffers;

namespace MySuperSocketCore
{
    public interface IPipelineFilter
    {
        AnalyzedPacket Filter(ref ReadOnlySequence<byte> buffer);

        IPipelineFilter NextFilter { get; }
    }
}