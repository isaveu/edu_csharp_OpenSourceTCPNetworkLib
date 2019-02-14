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
using MySuperSocketKestrelCore;

namespace TestApp
{
    class Program
    {
        static IServer CreateSocketServer<TPipelineFilter>(Dictionary<string, string> configDict = null, Action<AppSession, AnalyzedPacket> packageHandler = null)
            where TPipelineFilter : IPipelineFilter, new()
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

            services.AddLogging();

            RegisterServices(services);

            server.Configure<TPipelineFilter>(serverOptions, services, packageHandler: packageHandler);
           
            return server;
        }

        static void RegisterServices(IServiceCollection services)
        {
            services.AddSingleton<ITransportFactory, SocketTransportFactory>();
        }

        static void Main(string[] args)
        {
            var server = CreateSocketServer<LinePipelineFilter>(packageHandler: async (s, p) => 
            {
                await s.Channel.SendAsync(p.Body.AsSpan());                
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
