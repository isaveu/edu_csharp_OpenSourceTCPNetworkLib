using System;
using System.Buffers;
using System.Threading.Tasks;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;


namespace MySuperSocketKestrelCore
{
    public class PipeChannel<TPackageInfo> : ChannelBase<TPackageInfo>, IChannel<TPackageInfo>, IChannel
        where TPackageInfo : class
    {
        private IPipelineFilter<TPackageInfo> _pipelineFilter;

        private TransportConnection _transportConnection;
        
        public PipeChannel(TransportConnection transportConnection, IPipelineFilter<TPackageInfo> pipelineFilter)
        {
            _transportConnection = transportConnection;
            _pipelineFilter = pipelineFilter;
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
                Console.WriteLine($"Dis Connected: {_transportConnection.ConnectionId} , threadId:{System.Threading.Thread.CurrentThread.ManagedThreadId}");
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
            var pipe = _transportConnection.Transport;
            pipe.Output.Write(buffer);  //WriteAsyn를 사용하면 아래의 FlushAsync 사용할 필요 없음
            return FlushAsync(pipe.Output);
        }

        async Task FlushAsync(PipeWriter buffer)
        {
            await buffer.FlushAsync();
        }
    }
}
