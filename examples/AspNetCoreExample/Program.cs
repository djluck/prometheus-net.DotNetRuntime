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
using Prometheus.Advanced;
using Prometheus.DotNetRuntime;

namespace AspNetCoreExample
{
    public class Program
    {
        
        public static void Main(string[] args)
        {
            DefaultCollectorRegistry.Instance.RegisterOnDemandCollectors(DotNetRuntimeStatsBuilder.Default());
            var metricServer = new MetricServer(12204);
            metricServer.Start();
            
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}