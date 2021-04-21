using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prometheus;
using Prometheus.DotNetRuntime;

namespace AspNetCoreExample
{
    public class Startup
    {
        private static Options _options;
        public static IDisposable Collector;
        private static ILogger<Startup> _logger;

        public Startup(IConfiguration configuration, ILogger<Startup> logger)
        {
            Configuration = configuration;

            _options = new Options();
            _logger = logger;
            configuration.Bind("Example", _options);

            if (_options.EnableMetrics)
            {
                Collector = CreateCollector();
            }
            else
                logger.LogWarning($"prometheus-net.DotNetRuntime was NOT started- {_options.EnableMetrics} was set to false");

            if (_options.MinThreadPoolSize.HasValue)
            {
                logger.LogInformation($"Setting minimum thread pool size of {_options.MinThreadPoolSize.Value}");
                ThreadPool.SetMinThreads(_options.MinThreadPoolSize.Value, 1);
            }
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            
            app.UseRouting(); 
            
            app.UseEndpoints(endpoints =>
            {
                // Mapping of endpoints goes here:
                endpoints.MapControllers();
            });
            
            app.UseMetricServer();
        }
        
        public static IDisposable CreateCollector()
        {
            _logger.LogInformation($"Configuring prometheus-net.DotNetRuntime: will recycle event listeners every {_options.RecycleEvery} ({_options.RecycleEvery.TotalSeconds:N0} seconds).");

            var builder = DotNetRuntimeStatsBuilder.Default();
            
            if (!_options.UseDefaultMetrics)
            {
                builder = DotNetRuntimeStatsBuilder.Customize()
                    .WithContentionStats(CaptureLevel.Informational)
                    .WithGcStats(CaptureLevel.Verbose)
                    .WithThreadPoolStats(CaptureLevel.Informational)
                    .WithExceptionStats(CaptureLevel.Errors)
                    .WithJitStats()
                    .WithKestrelStats(CaptureLevel.Informational);
            }
            
            builder 
#if NET5_0
                .RecycleCollectorsEvery(_options.RecycleEvery)
#endif
                .WithErrorHandler(ex => _logger.LogError(ex, "Unexpected exception occurred in prometheus-net.DotNetRuntime"));

            if (_options.UseDebuggingMetrics)
            {
                _logger.LogInformation("Using debugging metrics.");
                builder.WithDebuggingMetrics(true);
            }

            _logger.LogInformation("Starting prometheus-net.DotNetRuntime...");
            
            return builder
                .StartCollecting();
        }
    }
}