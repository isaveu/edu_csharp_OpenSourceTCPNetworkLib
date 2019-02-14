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

        const Int64 ENABLE_SENDING = 1;
        Int64 IsEnableSending = ENABLE_SENDING;
        TcpSendState CurSendState = TcpSendState.NORMAL;
        Int64 CurrentSendingLength = 0;

        Int32 MaxRecvPacketSize = 0;
        Int32 MaxRecvBufferSize = 0;

        Int32 MaxSendPacketSize = 0;
        Int32 MaxSendingSize = 0;
        Int32 MaxSendReTryCount = 0;        


        public TcpPipeChannel(Socket socket, IPipelineFilter pipelineFilter)
            : base(pipelineFilter)
        {
            _socket = socket;
        }

        public override void SetSendOption(int maxPacketSize, int maxSendingSize, int maxReTryCount)
        {
            MaxSendPacketSize = maxPacketSize;
            MaxSendingSize = maxSendingSize;
            MaxSendReTryCount = maxReTryCount;
        }

        public override void SetRecvOption(int maxPacketSize, int maxBufferSize)
        {
            MaxRecvPacketSize = maxPacketSize;
            MaxRecvBufferSize = maxBufferSize;
        }

        private async Task FillPipeAsync(Socket socket, PipeWriter writer)
        {
            while (true)
            {
                try
                {
                    Memory<byte> memory = writer.GetMemory(MaxRecvBufferSize);
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
                       
        public override async Task<int> SendAsync(ReadOnlyMemory<byte> buffer)
        {
            if(IsEnableSend() == false)
            {
                return 0;
            }

            return await _socket.SendAsync(GetArrayByMemory(buffer), SocketFlags.None);
        }

        public override async Task<int> SendAsync(ArraySegment<byte> buffer)
        {
            if (IsEnableSend() == false)
            {
                return 0;
            }

            return await _socket.SendAsync(buffer, SocketFlags.None);
        }

        public override void SendTask(ReadOnlyMemory<byte> buffer)
        {
            if (IsEnableSend() == false)
            {
                return;
            }

            Task.Run(() => RealSend(buffer));
        }

        public override void SendTask(ArraySegment<byte> buffer)
        {
            if (IsEnableSend() == false)
            {
                return;
            }

            Task.Run(() => RealSend(buffer));
        }

        //TODO 일부러 send buffer를 작게 잡고, 클라이언트는 redv 버퍼를 작게 잡고, receive를 하지 않도록 해본다
        async Task RealSend(ReadOnlyMemory<byte> buffer)
        {
            if(IsSendingTooMuch())
            {
                SetInvalideSendState(TcpSendState.SENDING_SIZE_OVER);
                return;
            }
            
            System.Threading.Interlocked.Add(ref CurrentSendingLength, buffer.Length);
                        
            var tryCount = 1;
            while (IsEnableSend() && tryCount <= MaxSendReTryCount)
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
                        ++tryCount;

                        if (sendLen == 0)
                        {
                            continue;
                        }
                            
                        buffer = buffer.Slice(sendLen, (expertLen - sendLen));
                    }
                }
                catch
                {
                    SetInvalideSendState(TcpSendState.RISE_EXCEPTION);
                    return;
                }
            }

            SetInvalideSendState(TcpSendState.RE_TRY_OVER);
        }

        void SetInvalideSendState(TcpSendState state)
        {
            if(System.Threading.Interlocked.CompareExchange(ref IsEnableSending, 0, 1) == 0)
            {
                CurSendState = state;
            }
        }

        bool IsEnableSend()
        {
            return System.Threading.Interlocked.Read(ref IsEnableSending) == ENABLE_SENDING;
        }

        bool IsSendingTooMuch()
        {
            return System.Threading.Interlocked.Read(ref CurrentSendingLength) > MaxSendingSize;            
        }

    } 
    
    
    public enum TcpSendState
    {
        NORMAL = 0,
        SENDING_SIZE_OVER = 1,
        RE_TRY_OVER = 2, 
        RISE_EXCEPTION = 3
    }
}
