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

        //  OnConnection는 Kestrel의 SocketTransPort.cs의 RunAcceptLoopAsync에서 호출하고 있다.
        public Task OnConnection(TransportConnection connection)
        {
            //TODO 옵션에서 리드, 라이트 버퍼 크기 얻어와야 된다
            var inputOptions = GetInputPipeOptions(connection.InputWriterScheduler, 0, connection.MemoryPool, connection.InputWriterScheduler);
            var outputOptions = GetOutputPipeOptions(connection.OutputReaderScheduler, 0, connection.MemoryPool, connection.OutputReaderScheduler);

            var pair = DuplexPipe.CreateConnectionPair(inputOptions, outputOptions);

            // TODO 숫자 기반 uid로 바꾼다
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
                Interlocked.Decrement(ref _sessionCount); //TODO 에러인 듯. catch 절에서 호출해야 된다
            }
        }


        internal static PipeOptions GetInputPipeOptions(PipeScheduler pipeScheduler, int maxRequestBufferSize, MemoryPool<byte> memoryPool, PipeScheduler writerScheduler)
        {
            var temp = new PipeOptions();

            return new PipeOptions
            (

                //TODO maxRequestBufferSize을 null로 했을 때 기본 값 들어가는지 확인 필요
                pool: memoryPool,
                readerScheduler: pipeScheduler,
                writerScheduler: writerScheduler,
                pauseWriterThreshold: maxRequestBufferSize == 0 ? temp.PauseWriterThreshold : maxRequestBufferSize,
                resumeWriterThreshold: maxRequestBufferSize == 0 ? temp.ResumeWriterThreshold : maxRequestBufferSize,
                useSynchronizationContext: false,
                minimumSegmentSize: KestrelMemoryPool.MinimumSegmentSize  //TODO 이 크기가 패킷의 최소 크기가 되어야 한다
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