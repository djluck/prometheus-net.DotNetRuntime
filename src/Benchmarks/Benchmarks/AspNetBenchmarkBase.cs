using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Prometheus.DotNetRuntime;

namespace Benchmarks.Benchmarks
{
    [BenchmarkCategory("aspnet")]
    public abstract class AspNetBenchmarkBase
    {
        public const int NumRequests = 10_000;
            
        private IWebHost webHost;
        private CancellationTokenSource ctSource = new CancellationTokenSource();
        private HttpClient client;
        private byte[][] buffers;
        private Task webHostTask;
        
        public int NumHttpConnections { get; set; } = 50;

        [GlobalSetup]
        public void GlobalSetup()
        {
            PreGlobalSetup();
            webHost = WebHost.CreateDefaultBuilder()
                .UseStartup<Startup>()
                .ConfigureKestrel(cfg => { cfg.ListenLocalhost(5000); })
                .Build();
            webHostTask = webHost.RunAsync(ctSource.Token);

            // preallocate buffers to avoid having them counted as part of each benchmark run
            buffers = new byte[NumHttpConnections][];
            for (int i = 0; i < buffers.Length; i++)
                buffers[i] = new byte[1024 * 64];

            client = new HttpClient(new SocketsHttpHandler() { MaxConnectionsPerServer = NumHttpConnections });
        }

        protected virtual void PreGlobalSetup()
        {
        }

        protected virtual void PostGlobalCleanup()
        {
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            ctSource.Cancel();
            client.Dispose();
            webHostTask.Wait();
            PostGlobalCleanup();
        }

        protected async Task MakeHttpRequests()
        {
            var requestsRemaining = NumRequests;
            var tasks = Enumerable.Range(0, NumHttpConnections)
                .Select(async n =>
                {
                    var buffer = buffers[n];
                    while (Interlocked.Decrement(ref requestsRemaining) > 0)
                    {
                        await using var response = await client.GetStreamAsync("http://localhost:5000/api/benchmark");
                        while (await response.ReadAsync(buffer, 0, buffer.Length) > 0)
                        {
                        }
                    }
                });

            await Task.WhenAll(tasks);
        }
    }
}