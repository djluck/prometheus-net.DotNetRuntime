using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Prometheus;
using Prometheus.DotNetRuntime;

namespace AspNetCoreExample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (Environment.GetEnvironmentVariable("NOMON") == null)
            {
                Console.WriteLine("Enabling prometheus-net.DotNetStats...");
                DotNetRuntimeStatsBuilder.Customize()
                    .WithThreadPoolSchedulingStats()
                    .WithContentionStats()
                    .WithGcStats()
                    .WithJitStats()
                    .WithThreadPoolStats()
                    .WithExceptionStats()
                    .WithErrorHandler(ex => Console.WriteLine("ERROR: " + ex.ToString()))
                    //.WithDebuggingMetrics(true);
                    .StartCollecting();
            }

            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureKestrel(opts =>
                {
                    opts.AllowSynchronousIO = true;
                })
                .UseStartup<Startup>();
    }
}