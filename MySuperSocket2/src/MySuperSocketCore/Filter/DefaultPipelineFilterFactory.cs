using System;
using System.Buffers;

namespace MySuperSocketCore
{
    public class DefaultPipelineFilterFactory<TPipelineFilter> : IPipelineFilterFactory
        where TPipelineFilter : IPipelineFilter, new()
    {
        public IPipelineFilter Create(object client)
        {
            return new TPipelineFilter();
        }
    }
}