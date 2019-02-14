using System;
using System.Threading.Tasks;

namespace MySuperSocketKestrelCore
{
    public interface IListenerFactory
    {
        IListener CreateListener(ListenOptions options, object pipelineFilterFactory);
    }
}