using System;
using System.Buffers;

namespace MySuperSocketKestrelCore
{
    public abstract class PipelineFilterBase<TPackageInfo> : IPipelineFilter<TPackageInfo>
        where TPackageInfo : class
    {
        public IPipelineFilter<TPackageInfo> NextFilter { get; protected set; }

        public abstract TPackageInfo Filter(ref ReadOnlySequence<byte> buffer);
    }
}