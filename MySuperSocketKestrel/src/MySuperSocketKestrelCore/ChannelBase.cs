using System;
using System.Threading.Tasks;

namespace MySuperSocketKestrelCore
{
    public abstract class ChannelBase : IChannel
    {
        public abstract Task ProcessRequest();
        public abstract Task SendAsync(ReadOnlySpan<byte> data);

        private Action<IChannel, AnalyzedPacket> _packageReceived;

        public event Action<IChannel, AnalyzedPacket> PackageReceived
        {
            add { _packageReceived += value; }
            remove { _packageReceived -= value; }
        }

        protected void OnPackageReceived(AnalyzedPacket package)
        {
            _packageReceived?.Invoke(this, package);
        }

        private EventHandler _closed;

        public event EventHandler Closed
        {
            add { _closed += value; }
            remove { _closed -= value; }
        }

        protected virtual void OnClosed()
        {
            _closed?.Invoke(this, EventArgs.Empty);
        }
    }
}
