using System;
using System.Buffers;
using System.Threading.Tasks;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.Extensions.Logging;

namespace MySuperSocketKestrelCore
{
    // 파일 이름도 바꾸기
    public class TcpPipeChannel : ChannelBase
    {
        private IPipelineFilter _pipelineFilter;

        private TransportConnection _transportConnection;


        const Int64 ENABLE_SENDING = 1;
        Int64 IsEnableSending = ENABLE_SENDING;
        TcpSendState CurSendState = TcpSendState.NORMAL;
        Int64 CurrentSendingLength = 0;

        Int32 MaxSendPacketSize = 0;
        Int32 MaxSendingSize = 0;
        Int32 MaxSendReTryCount = 0;


        public TcpPipeChannel(TransportConnection transportConnection, IPipelineFilter pipelineFilter)
        {
            _transportConnection = transportConnection;
            _pipelineFilter = pipelineFilter;
        }

        public override void SetSendOption(int maxPacketSize, int maxSendingSize, int maxReTryCount)
        {
            MaxSendPacketSize = maxPacketSize;
            MaxSendingSize = maxSendingSize;
            MaxSendReTryCount = maxReTryCount;
        }


        public override async Task ProcessRequest()
        {
            //TODO 패킷이 미완성 상태로 왔을 때 다음에 받는 것과 잘 조립 되는지 확인 필요하다
            var input = _transportConnection.Transport.Input;//_transportConnection.Application.Input;
            var currentPipelineFilter = _pipelineFilter;
                        
            try
            {
                while (true)
                {
                    var result = await input.ReadAsync();
                    var buffer = result.Buffer;

                    if (result.IsCompleted)
                    {
                        OnClosed();
                        break;
                    }

                    while (true)
                    {
                        var packageInfo = currentPipelineFilter.Filter(ref buffer);

                        if (currentPipelineFilter.NextFilter != null)
                        {
                            _pipelineFilter = currentPipelineFilter = currentPipelineFilter.NextFilter;
                        }

                        if (packageInfo == null)
                        {
                            break;
                        }

                        OnPackageReceived(packageInfo);

                        if (buffer.Length == 0)
                        {
                            break;
                        }
                    }

                    // AdvanceTo를 안하면 다음 ReadAsync에서 에러 발생으로 접속이 끊어진다.
                    input.AdvanceTo(buffer.Start, buffer.End);                    
                }
            }
            catch // 접속이 끊어지면 catch가 호출된다
            {
                OnClosed();
                GLogging.Logger().LogDebug($"Dis Connected: {_transportConnection.ConnectionId} , threadId:{System.Threading.Thread.CurrentThread.ManagedThreadId}");
            }
            finally
            {                    
                input.Complete();

                //가끔 여기서 예외발생. Kesterl의 SocketConnection 에서  ThreadPool.UnsafeQueueUserWorkItem 쯤에서 발생
                //크게 중요하지는 않는 듯 하다. Send를 하지 않는데 Complet 시켜서 그렇지 않을까라고 생각한다
                // 주석 처리하면 예외 발생하지 않고, 접속 끊어지는 것도 잘됨. 
                //TODO echo 테스트 등에서도 문제 없는지 확인 필요
                //_transportConnection.Transport.Output.Complete(); 
            }
            
            await Task.CompletedTask;
        }

        public override Task SendAsync(ReadOnlySpan<byte> buffer)
        {
            if(IsEnableSend() == false)
            {
                return Task.CompletedTask;
            }

            //if (IsSendingTooMuch())
            //{
            //    SetInvalideSendState(TcpSendState.SENDING_SIZE_OVER);
            //    return Task.CompletedTask;
            //}

            //TODO pipe.Output의 남은 양을 조사해서 크면 중단하도록 해야 한다.

            var pipe = _transportConnection.Transport;
            pipe.Output.Write(buffer);  //WriteAsyn를 사용하면 아래의 FlushAsync 사용할 필요 없음
            return FlushAsync(pipe.Output);
        }
        public override Task SendAsync(ArraySegment<byte> buffer)
        {
            return SendAsync((ReadOnlySpan<byte>)buffer);
        }

        async Task FlushAsync(PipeWriter buffer)
        {
            await buffer.FlushAsync();
        }


        void SetInvalideSendState(TcpSendState state)
        {
            if (System.Threading.Interlocked.CompareExchange(ref IsEnableSending, 0, 1) == 0)
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
