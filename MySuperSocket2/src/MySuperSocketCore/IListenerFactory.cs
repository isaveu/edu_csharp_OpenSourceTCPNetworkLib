using System;
using System.Threading.Tasks;

namespace MySuperSocketCore
{
    public interface IListenerFactory
    {
        IListener CreateListener(ListenOptions options, object pipelineFilterFactory);
    }
}