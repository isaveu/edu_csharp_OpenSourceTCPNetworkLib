using System;
using System.Threading.Tasks;

namespace MySuperSocketKestrelCore
{
    public delegate void NewClientAcceptHandler(IListener listener, ChannelBase channel);

    public interface IListener
    {
        ListenOptions Options { get; }
        bool Start();
        event NewClientAcceptHandler NewClientAccepted;
        Task StopAsync();
    }
}