using System;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using System.Threading;
using System.Buffers;

namespace MySuperSocketKestrelCore
{
    public interface ISuperSocketConnectionDispatcher : IConnectionDispatcher
    {
        int SessionCount { get; }
    }

    public class ConnectionDispatcher<TPipelineFilter> : ISuperSocketConnectionDispatcher
        where TPipelineFilter : IPipelineFilter, new()
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

            //TODO �ڵ鷯�� ���� �� �ڵ鷯 �Լ��� SuperSocketServer���� �����Ų��. Session ��ü�� ��ȯ�ϵ��� �Ѵ�.

            var session = new AppSession(connection, new TPipelineFilter());

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


        public static PipeOptions GetInputPipeOptions(PipeScheduler pipeScheduler, int maxRequestBufferSize, MemoryPool<byte> memoryPool, PipeScheduler writerScheduler)
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

        public static PipeOptions GetOutputPipeOptions(PipeScheduler pipeScheduler, int maxRequestBufferSize, MemoryPool<byte> memoryPool, PipeScheduler readerScheduler)
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


    internal static class CorrelationIdGenerator
    {
        // Base32 encoding - in ascii sort order for easy text based sorting
        private static readonly string _encode32Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUV";

        // Seed the _lastConnectionId for this application instance with
        // the number of 100-nanosecond intervals that have elapsed since 12:00:00 midnight, January 1, 0001
        // for a roughly increasing _lastId over restarts
        private static long _lastId = DateTime.UtcNow.Ticks;

        public static string GetNextId() => GenerateId(Interlocked.Increment(ref _lastId));

        private static unsafe string GenerateId(long id)
        {
            // The following routine is ~310% faster than calling long.ToString() on x64
            // and ~600% faster than calling long.ToString() on x86 in tight loops of 1 million+ iterations
            // See: https://github.com/aspnet/Hosting/pull/385

            // stackalloc to allocate array on stack rather than heap
            char* charBuffer = stackalloc char[13];

            charBuffer[0] = _encode32Chars[(int)(id >> 60) & 31];
            charBuffer[1] = _encode32Chars[(int)(id >> 55) & 31];
            charBuffer[2] = _encode32Chars[(int)(id >> 50) & 31];
            charBuffer[3] = _encode32Chars[(int)(id >> 45) & 31];
            charBuffer[4] = _encode32Chars[(int)(id >> 40) & 31];
            charBuffer[5] = _encode32Chars[(int)(id >> 35) & 31];
            charBuffer[6] = _encode32Chars[(int)(id >> 30) & 31];
            charBuffer[7] = _encode32Chars[(int)(id >> 25) & 31];
            charBuffer[8] = _encode32Chars[(int)(id >> 20) & 31];
            charBuffer[9] = _encode32Chars[(int)(id >> 15) & 31];
            charBuffer[10] = _encode32Chars[(int)(id >> 10) & 31];
            charBuffer[11] = _encode32Chars[(int)(id >> 5) & 31];
            charBuffer[12] = _encode32Chars[(int)id & 31];

            // string ctor overload that takes char*
            return new string(charBuffer, 0, 13);
        }
    }
}