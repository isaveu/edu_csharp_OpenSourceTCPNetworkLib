using System;
using System.Buffers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

namespace KestrelTcpServer
{
    public class SessionHandler : ConnectionHandler
    {
        public override async Task OnConnectedAsync(ConnectionContext connection)
        {
            try
            {
                //새로운 접속이 있을 때 마다 connection 객체는 새로 생성되나? YES
                //TODO 보내기 관련해서 디렉토리의 스크린샷 찍은 코드 참조하기

                Console.WriteLine($"New Connected: {connection.ConnectionId} , threadId:{System.Threading.Thread.CurrentThread.ManagedThreadId}");
                //_logger.LogInformation(connection.ConnectionId + " connected");

                while (true)
                {
                    var result = await connection.Transport.Input.ReadAsync();

                    if (result.IsCompleted)
                    {
                        break;
                    }

                    var buffer = result.Buffer;

                    if(buffer.IsEmpty == false)
                    {
                        Console.WriteLine($"{buffer.ToArray()}");
                    }

                    connection.Transport.Input.AdvanceTo(buffer.End);
                }
                Console.WriteLine($"Dis Connected: {connection.ConnectionId} , threadId:{System.Threading.Thread.CurrentThread.ManagedThreadId}");

                //while (true)
                //{
                //    var result = await connection.Transport.Input.ReadAsync();
                //    var buffer = result.Buffer;

                //    foreach (var segment in buffer)
                //    {
                //        await connection.Transport.Output.WriteAsync(segment);
                //    }

                //    if (result.IsCompleted)
                //    {
                //        break;
                //    }

                //    connection.Transport.Input.AdvanceTo(buffer.End);
                //}

                //_logger.LogInformation(connection.ConnectionId + " disconnected");
            }
            catch // 접속이 끊어지면 catch가 호출된다
            {
                Console.WriteLine($"Dis Connected: {connection.ConnectionId} , threadId:{System.Threading.Thread.CurrentThread.ManagedThreadId}");
            }
            finally
            {
                // Today, Kestrel expects the ConnectionHandler to complete the transport pipes
                // this will be resolved in a future release

                // We're done reading
                connection.Transport.Input.Complete();

                // We're done writing
                connection.Transport.Output.Complete();
            }
        }
    }
}
