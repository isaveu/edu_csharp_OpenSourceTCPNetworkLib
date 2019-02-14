using System;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Buffers;
using System.Threading;
using System.Net;

namespace MySuperSocketKestrelCore
{
    public class TcpSocketListener : IListener
    {
        private Socket _listenSocket;

        private CancellationTokenSource _cancellationTokenSource;
        private TaskCompletionSource<bool> _stopTaskCompletionSource;
        private Func<Socket, ChannelBase> _channelFactory;
        
        public ListenOptions Options { get; private set; }

        public TcpSocketListener(ListenOptions options, Func<Socket, ChannelBase> channelFactory)
        {
            Options = options;
            _channelFactory = channelFactory;
        }

        private IPEndPoint GetListenEndPoint(string ip, int port)
        {
            var ipAddress = IPAddress.None;

            if ("any".Equals(ip, StringComparison.OrdinalIgnoreCase))
            {
                ipAddress = IPAddress.Any;
            }
            else if ("IpV6Any".Equals(ip, StringComparison.OrdinalIgnoreCase))
            {
                ipAddress = IPAddress.IPv6Any;
            }
            else
            {
                ipAddress = IPAddress.Parse(ip);
            }

            return new IPEndPoint(ipAddress, port);
        }

        public bool Start()
        {
            var options = Options;

            try
            {
                var listenEndpoint = GetListenEndPoint(options.Ip, options.Port);
                var listenSocket = _listenSocket = new Socket(listenEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                
                listenSocket.Bind(listenEndpoint);
                listenSocket.Listen(options.BackLog);

                listenSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

                if (options.NoDelay)
                    listenSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);

                _cancellationTokenSource = new CancellationTokenSource();

                KeepAccept(listenSocket).Start();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private async Task KeepAccept(Socket listenSocket)
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                var socket = await listenSocket.AcceptAsync();
                OnNewClientAccept(socket);
            }

            _stopTaskCompletionSource.TrySetResult(true);
        }

        public event NewClientAcceptHandler NewClientAccepted;

        private void OnNewClientAccept(Socket socket)
        {
            var handler = NewClientAccepted;

            if (handler == null)
                return;

            Task.Run(() => handler(this, _channelFactory(socket)));
        }

        public Task StopAsync()
        {
            _stopTaskCompletionSource = new TaskCompletionSource<bool>();
            _cancellationTokenSource.Cancel();
            _listenSocket.Close();
            
            return _stopTaskCompletionSource.Task;
        }
    }
}