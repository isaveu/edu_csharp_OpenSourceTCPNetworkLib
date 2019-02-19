using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace MySuperSocketCore
{
    //TODO ��Ŷ ���ڵ��� body�� ������ �ѱ�� ��, ���� �Ҵ� �� �����ϴ� ���� ���� �־ �Ѵ�
    //TODO Send�� ArraySegment Ȥ�� �޸�Ǯ�� ����Ͽ� send �� �޸� �� �������.
    //TODO messagepack�� span ����. �̰��� ����ؾ� �Ѵ� https://github.com/AArnott/MessagePack-CSharp
    //TODO ���뼺 ���̱�

    public class SuperSocketServer : IServer
    {
        public Action<AppSession> NetEventOnConnect;
        public Action<AppSession> NetEventOnCloese;
        public Action<AppSession, AnalyzedPacket> NetEventOnReceive;
        
        private IServiceCollection _serviceCollection;

        private IServiceProvider _serviceProvider;

        public ServerOptions Options { get; private set; }

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// Server's name
        /// </summary>
        /// <returns>the name of the server instance</returns>
        public string Name 
        {
            get { return Options.Name; }
        }

        private IList<IListener> _listeners;
        
        protected internal ILoggerFactory LoggerFactory { get; private set; }

        static public Microsoft.Extensions.Logging.ILogger GetLogger() { return _logger; }
        static private Microsoft.Extensions.Logging.ILogger _logger;

        private bool _configured = false;

        private int _sessionCount;

        C3SockNetUtil.IUniqueIdGenerator SessionUniqueIdGen = new C3SockNetUtil.UniqueIdGenSimple();


        public void CreateSocketServer(ServerBuildParameter parameter, List<IPipelineFilterFactory> pipelineFilterFactoryList)
        {
            NetEventOnConnect = parameter.NetEventOnConnect;
            NetEventOnCloese = parameter.NetEventOnCloese;
            NetEventOnReceive = parameter.NetEventOnReceive;

            var services = new ServiceCollection();
            services.AddLogging();

            Configure(parameter.serverOption, services, pipelineFilterFactoryList);            
        }

        public bool Configure(ServerOptions options, IServiceCollection services, 
                                                    List<IPipelineFilterFactory> pipelineFilterFactoryList)
        { 
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (pipelineFilterFactoryList == null)
            {
                throw new ArgumentNullException(nameof(pipelineFilterFactoryList));
            }


            Options = options;            
            _serviceCollection = services.AddOptions(); 
            _serviceProvider = services.BuildServiceProvider();

            LoggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();
            LoggerFactory.AddNLog();
            _logger = LoggerFactory.CreateLogger("SuperSocket");
            GLogging.SetLogger(_logger);


             var listenerFactory = _serviceProvider.GetService<IListenerFactory>();
            if (listenerFactory == null)
            {
                listenerFactory = new TcpSocketListenerFactory();
            }

            _listeners = new List<IListener>();

            var index = 0;
            foreach (var l in options.Listeners)
            {
                var listener = listenerFactory.CreateListener(l, pipelineFilterFactoryList[index]);
                listener.NewClientAccepted += OnNewClientAccept;
                _listeners.Add(listener);

                ++index;
            }

            return _configured = true;
        }

        protected virtual void OnNewClientAccept(IListener listener, ChannelBase channel)
        {
            var option = listener.Options;
            channel.SetRecvOption(option.MaxRecvPacketSize, option.MaxReceivBufferSize);
            channel.SetSendOption(option.MaxSendPacketSize, option.MaxSendingSize, option.MaxReTryCount);

            //TODO �� ���� ��ü�� �����̳ʿ� �����ϰ� �ִٰ�. �� �Լ��� ���� �� �����ؾ� �Ѵ�.
            var session = new AppSession(NextSessionUniqueId(), channel);

            NetEventOnConnect(session);

            HandleSession(session).Wait();

            NetEventOnCloese(session);
        }

      
        private async Task HandleSession(AppSession session)
        {
            void OnPackageReceived(AnalyzedPacket packet)
            {
                NetEventOnReceive(session, packet);
            }

            Interlocked.Increment(ref _sessionCount);
            session.Channel.OnPackageReceived = OnPackageReceived;
            try
            {
                // _logger.LogInformation($"A New session connected: {session.SessionID}");                
                await session.Channel.ProcessRequest();
               // _logger.LogInformation($"The session disconnected: {session.SessionID}");
            }
            catch (Exception e)
            {
                _logger.LogError($"Failed to handle the session {session.SessionID}.", e);
            }
            finally
            {
                Interlocked.Decrement(ref _sessionCount);
            }           
        }  

        public int SessionCount
        {
            get { return _sessionCount; }
        }

        public async Task<bool> StartAsync()
        {
            await Task.Delay(0); // async ���� ��� ���ֱ� ���� ����

            if (!_configured)
                _logger.LogError("The server has not been initialized successfully!");

            var binded = 0;

            foreach (var listener in _listeners)
            {
                try
                {
                    listener.Start();
                    binded++;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Failed to bind the transport {listener.ToString()}.");
                }
            }

            if (binded == 0)
            {
                _logger.LogCritical("No transport binded successfully.");
                return false;
            }

            return true;
        }

        public async Task StopAsync()
        {
            var tasks = _listeners.Select(l => l.StopAsync()).ToArray();
            await Task.WhenAll(tasks);
        }

        public UInt64 NextSessionUniqueId() { return SessionUniqueIdGen.NextId();  }
       


    }
}