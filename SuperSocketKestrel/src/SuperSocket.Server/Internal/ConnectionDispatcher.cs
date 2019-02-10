using System;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading.Tasks;
using SuperSocket.Channel;
using SuperSocket.ProtoBase;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using System.Threading;
using System.Buffers;

namespace SuperSocket.Server
{
    internal interface ISuperSocketConnectionDispatcher : IConnectionDispatcher
    {
        int SessionCount { get; }
    }

    internal class ConnectionDispatcher<TPackageInfo, TPipelineFilter> : ISuperSocketConnectionDispatcher
        where TPackageInfo : class
        where TPipelineFilter : IPipelineFilter<TPackageInfo>, new()
    {
        private int _sessionCount;

        public int SessionCount
        {
            get { return _sessionCount; }
        }

        //  OnConnection�� Kestrel�� SocketTransPort.cs�� RunAcceptLoopAsync���� ȣ���ϰ� �ִ�.
        public Task OnConnection(TransportConnection connection)
        {
            //TODO �ɼǿ��� ����, ����Ʈ ���� ũ�� ���;� �ȴ�
            var inputOptions = GetInputPipeOptions(connection.InputWriterScheduler, 0, connection.MemoryPool, connection.InputWriterScheduler);
            var outputOptions = GetOutputPipeOptions(connection.OutputReaderScheduler, 0, connection.MemoryPool, connection.OutputReaderScheduler);

            var pair = DuplexPipe.CreateConnectionPair(inputOptions, outputOptions);

            // TODO ���� ��� uid�� �ٲ۴�
            connection.ConnectionId = CorrelationIdGenerator.GetNextId();
            connection.Transport = pair.Transport;

            // This *must* be set before returning from OnConnection
            connection.Application = pair.Application;


            var session = new AppSession<TPackageInfo>(connection, new TPipelineFilter());

            Interlocked.Increment(ref _sessionCount);

            try
            {                
                return session.ProcessRequest();
            }
            finally
            {
                Interlocked.Decrement(ref _sessionCount); //TODO ������ ��. catch ������ ȣ���ؾ� �ȴ�
            }
        }


        internal static PipeOptions GetInputPipeOptions(PipeScheduler pipeScheduler, int maxRequestBufferSize, MemoryPool<byte> memoryPool, PipeScheduler writerScheduler)
        {
            var temp = new PipeOptions();

            return new PipeOptions
            (

                //TODO maxRequestBufferSize�� null�� ���� �� �⺻ �� ������ Ȯ�� �ʿ�
                pool: memoryPool,
                readerScheduler: pipeScheduler,
                writerScheduler: writerScheduler,
                pauseWriterThreshold: maxRequestBufferSize == 0 ? temp.PauseWriterThreshold : maxRequestBufferSize,
                resumeWriterThreshold: maxRequestBufferSize == 0 ? temp.ResumeWriterThreshold : maxRequestBufferSize,
                useSynchronizationContext: false,
                minimumSegmentSize: KestrelMemoryPool.MinimumSegmentSize  //TODO �� ũ�Ⱑ ��Ŷ�� �ּ� ũ�Ⱑ �Ǿ�� �Ѵ�
            );
        }

        internal static PipeOptions GetOutputPipeOptions(PipeScheduler pipeScheduler, int maxRequestBufferSize, MemoryPool<byte> memoryPool, PipeScheduler readerScheduler)
        {
            var temp = new PipeOptions();

            return new PipeOptions
            (
                pool: memoryPool,
                readerScheduler: readerScheduler,
                writerScheduler: pipeScheduler,
                pauseWriterThreshold: maxRequestBufferSize == 0 ? temp.PauseWriterThreshold : maxRequestBufferSize,
                resumeWriterThreshold: maxRequestBufferSize == 0 ? temp.ResumeWriterThreshold : maxRequestBufferSize,
                useSynchronizationContext: false,
                minimumSegmentSize: KestrelMemoryPool.MinimumSegmentSize
            );
        }
    }
}