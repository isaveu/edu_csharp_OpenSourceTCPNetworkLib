using System;

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;

namespace KestrelTcpServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseKestrel(options =>
                {
                    // TCP 11021
                    options.ListenLocalhost(11021, builder =>
                    {
                        builder.UseConnectionHandler<SessionHandler>();
                    });

                    //// HTTP 5000
                    //options.ListenLocalhost(5000);

                    //// HTTPS 5001
                    //options.ListenLocalhost(5001, builder =>
                    //{
                    //    builder.UseHttps();
                    //});
                })
                .UseStartup<Startup>();
    }
}
