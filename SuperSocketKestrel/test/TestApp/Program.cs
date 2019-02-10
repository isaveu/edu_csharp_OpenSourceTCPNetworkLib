using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using SuperSocket;
using SuperSocket.ProtoBase;
using SuperSocket.Server;

namespace TestApp
{
    class Program
    {
        static IServer CreateSocketServer<TPackageInfo, TPipelineFilter>(Dictionary<string, string> configDict = null, Action<IAppSession, TPackageInfo> packageHandler = null)
            where TPackageInfo : class
            where TPipelineFilter : IPipelineFilter<TPackageInfo>, new()
        {
            if (configDict == null)
            {
                configDict = new Dictionary<string, string>
                {
                    { "name", "TestServer" },
                    { "listeners:0:ip", "Any" },
                    { "listeners:0:port", "11021" },
                    { "listeners:0:backLog", "100" }
                };
            }

            var server = new SuperSocketServer();
            var services = new ServiceCollection();

            var builder = new ConfigurationBuilder().AddInMemoryCollection(configDict);
            var serverOptions = new ServerOptions();

            var config = builder.Build();
            config.Bind(serverOptions);

            //services.AddLogging(loggingBuilder => loggingBuilder.AddConsole() );  // 기본 콘솔 로그 사용 시
            services.AddLogging();

            RegisterServices(services);

            server.Configure<TPackageInfo, TPipelineFilter>(serverOptions, services, packageHandler: packageHandler);
           
            return server;
        }

        static void RegisterServices(IServiceCollection services)
        {
            services.AddSingleton<ITransportFactory, SocketTransportFactory>();
        }

        static void Main(string[] args)
        {
            var server = CreateSocketServer<LinePackageInfo, LinePipelineFilter>(packageHandler: async (s, p) => 
            {
                await s.SendAsync(Encoding.UTF8.GetBytes(p.Line).AsSpan());                
            });
            
            server.StartAsync().Wait();

            while (Console.ReadLine().ToLower() != "c")
            {
                continue;
            }

            server.StopAsync().Wait();
        }
    }
}
