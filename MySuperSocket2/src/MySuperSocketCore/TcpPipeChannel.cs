using System;
using System.Threading.Tasks;
using System.IO.Pipelines;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace MySuperSocketCore
{
    public class TcpPipeChannel : PipeChannel
    {
        Socket _socket;

        Int32 MaxPacketSize = 0;
        Int32 MaxSendingSize = 0;
        Int32 MaxReTryCount = 0;
        Int32 CurrentSendingLength = 0;

        public TcpPipeChannel(Socket socket, IPipelineFilter pipelineFilter)
            : base(pipelineFilter)
        {
            _socket = socket;
        }

        private async Task FillPipeAsync(Socket socket, PipeWriter writer)
        {
            const int minimumBufferSize = 512;

            while (true)
            {
                try
                {
                    Memory<byte> memory = writer.GetMemory(minimumBufferSize);
                    int bytesRead = await ReceiveAsync(socket, memory, SocketFlags.None);

                    if (bytesRead == 0)
                    {
                        break;
                    }

                    // Tell the PipeWriter how much was read
                    writer.Advance(bytesRead);
                }
                catch
                {
                    //GLogging.Logger().LogError($"Dis Connected: {socket.GetHashCode()} , threadId:{System.Threading.Thread.CurrentThread.ManagedThreadId}");
                    break;                    
                }

                // Make the data available to the PipeReader
                FlushResult result = await writer.FlushAsync();

                if (result.IsCompleted)
                {
                    break;
                }
            }

            // Signal to the reader that we're done writing
            writer.Complete();
        }

        private async Task<int> ReceiveAsync(Socket socket, Memory<byte> memory, SocketFlags socketFlags)
        {
            return await socket.ReceiveAsync(GetArrayByMemory((ReadOnlyMemory<byte>)memory), socketFlags);
        }

        public override async Task ProcessRequest()
        {
            var pipe = new Pipe();

            Task writing = FillPipeAsync(_socket, pipe.Writer);
            Task reading = ReadPipeAsync(pipe.Reader);

            await Task.WhenAll(reading, writing);

            _socket = null;
        }


        //TODO 옵션에 따라서 사용하도록 하자
        public override void SetSendLimit(int maxPacketSize, int maxSendingSize, int maxReTryCount)
        {
            MaxPacketSize = maxPacketSize;
            MaxSendingSize = maxSendingSize;
            MaxReTryCount = maxReTryCount;
        }

        public override async Task<int> SendAsync(ReadOnlyMemory<byte> buffer)
        {
            return await _socket.SendAsync(GetArrayByMemory(buffer), SocketFlags.None);
        }

        public override async Task<int> SendAsync(ArraySegment<byte> buffer)
        {
            return await _socket.SendAsync(buffer, SocketFlags.None);
        }

        public override void SendTask(ReadOnlyMemory<byte> buffer)
        {  
            Task.Run(() => RealSend(buffer));
        }

        public override void SendTask(ArraySegment<byte> buffer)
        {
            Task.Run(() => RealSend(buffer));
        }

        //TODO 일부러 send buffer를 잡게 잡고, 클라이언트는 redv 버퍼를 작게 잡고, receive를 하지 않도록 해본다
        async Task RealSend(ReadOnlyMemory<byte> buffer)
        {
            var enableSend = true;
            System.Threading.Interlocked.Add(ref CurrentSendingLength, buffer.Length);

            //TODO 지정 횟수 이상으로 보내기 완료가 안되면 에러로 판정. 완료하면 보내기 시도 횟수는 0으로 설정.            
            //TODO 현재 보내 예정인 양이 지정한 것보다 크면 이것도 에러로 판정.
            //TODO 위에서 에러가 나면 사용 불가로 설정.

            //TODO 무한 반복에 빠지지 않도록 해야 한다.
            while (enableSend)
            {
                try
                {
                    var expertLen = buffer.Length;
                    var sendLen = await _socket.SendAsync(GetArrayByMemory(buffer), SocketFlags.None);

                    System.Threading.Interlocked.Add(ref CurrentSendingLength, -sendLen);

                    if (expertLen == sendLen)
                    {
                        return;
                    }
                    else
                    {
                        buffer = buffer.Slice(sendLen, (expertLen - sendLen));
                    }                    
                }
                catch
                {
                    //TODO 에러 처리하고, 로그 남기기
                    return;
                }
            }

        }

        
}
