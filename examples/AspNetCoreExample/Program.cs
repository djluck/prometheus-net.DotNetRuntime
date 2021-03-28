using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Prometheus.DotNetRuntime;

namespace AspNetCoreExample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }
        
        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(opts =>
                {
                    opts.AddEnvironmentVariables("Example");
                })
                .ConfigureKestrel(opts =>
                {
                    opts.AllowSynchronousIO = true;
                })
                .UseStartup<Startup>();
    }
}