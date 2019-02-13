using System;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Buffers;

namespace MySuperSocketCore
{
    public class TcpSocketListenerFactory : IListenerFactory
    {
        public IListener CreateListener(ListenOptions options, object pipelineFilterFactory)
        { 
            var filterFactory = pipelineFilterFactory as IPipelineFilterFactory;

            ChannelBase ChannelFactoryFunc(Socket s)
            {
                var channel = new TcpPipeChannel(s, filterFactory.Create(s));
                return channel;
            }

            
            var listner = new TcpSocketListener(options, ChannelFactoryFunc);
            return listner;
        }

    }
}