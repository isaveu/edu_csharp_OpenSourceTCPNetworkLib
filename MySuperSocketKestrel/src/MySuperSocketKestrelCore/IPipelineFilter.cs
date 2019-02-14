using System;
using System.Buffers;

namespace MySuperSocketKestrelCore
{
    public interface IPipelineFilter<TPackageInfo>
        where TPackageInfo : class
    {
        TPackageInfo Filter(ref ReadOnlySequence<byte> buffer);

        IPipelineFilter<TPackageInfo> NextFilter { get; }
    }
}