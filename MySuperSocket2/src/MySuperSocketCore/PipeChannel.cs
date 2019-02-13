﻿using System;
using System.Buffers;
using System.Threading.Tasks;
using System.IO.Pipelines;
using System.Runtime.InteropServices;

namespace MySuperSocketCore
{
    public abstract class PipeChannel : ChannelBase
    {
        private IPipelineFilter _pipelineFilter;

        public PipeChannel(IPipelineFilter pipelineFilter)
        {
            _pipelineFilter = pipelineFilter;
        }

        protected internal ArraySegment<T> GetArrayByMemory<T>(ReadOnlyMemory<T> memory)
        {
            if (!MemoryMarshal.TryGetArray(memory, out var result))
            {
                throw new InvalidOperationException("Buffer backed by array was expected");
            }

            return result;
        }

        protected async Task ReadPipeAsync(PipeReader reader)
        {
            var currentPipelineFilter = _pipelineFilter;

            while (true)
            {
                var result = await reader.ReadAsync();
                var buffer = result.Buffer;

                try
                {
                    if (result.IsCompleted)
                    {
                        break;
                    }

                    while (true)
                    {
                        var packageInfo = currentPipelineFilter.Filter(ref buffer);

                        if (currentPipelineFilter.NextFilter != null)
                            _pipelineFilter = currentPipelineFilter = currentPipelineFilter.NextFilter;

                        // continue receive...
                        if (packageInfo == null)
                            break;

                        // already get a package
                        OnPackageReceived(packageInfo);

                        if (buffer.Length == 0) // no more data
                            break;
                    }
                }
                finally
                {
                    reader.AdvanceTo(buffer.Start, buffer.End);
                }
            }

            reader.Complete();
        }
    


    }
}
