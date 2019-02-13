using System;
using System.Buffers;

namespace MySuperSocketCore
{
    public abstract class PipelineFilterBase : IPipelineFilter
    {
        public IPipelineFilter NextFilter { get; protected set; }

        public abstract AnalyzedPacket Filter(ref ReadOnlySequence<byte> buffer);
    }
}