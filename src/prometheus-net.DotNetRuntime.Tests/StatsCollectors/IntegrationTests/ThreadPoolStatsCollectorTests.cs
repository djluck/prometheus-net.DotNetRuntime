using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Prometheus.DotNetRuntime.StatsCollectors;

namespace Prometheus.DotNetRuntime.Tests.StatsCollectors.IntegrationTests
{
    internal class ThreadPoolStatsCollectorTests : StatsCollectorIntegrationTestBase<ThreadPoolStatsCollector>
    {
        protected override ThreadPoolStatsCollector CreateStatsCollector()
        {
            return new ThreadPoolStatsCollector();
        }
        
        [Test]
        public async Task When_work_is_executed_on_the_thread_pool_then_executed_work_is_measured()
        {
            // schedule a bunch of blocking tasks that will make the thread pool will grow
            var tasks = Enumerable.Range(1, 1000)
                .Select(_ => Task.Run(() => Thread.Sleep(20)));

            await Task.WhenAll(tasks);

            Assert.That(() => StatsCollector.NumThreads.Value, Is.GreaterThanOrEqualTo(Environment.ProcessorCount).After(200, 10));
            Assert.That(StatsCollector.AdjustmentsTotal.CollectSingle().Sum(x => x.counter.value), Is.GreaterThanOrEqualTo(1));
        }
        
        [Test]
        public async Task When_IO_work_is_executed_on_the_thread_pool_then_the_number_of_io_threads_is_measured()
        {
            // need to schedule a bunch of IO work to make the IO pool grow
            using (var client = new HttpClient())
            {
                var httpTasks = Enumerable.Range(1, 30)
                    .Select(_ => client.GetAsync("http://google.com"));

                await Task.WhenAll(httpTasks);
            }
          
            Assert.That(() => StatsCollector.NumIocThreads.Value, Is.GreaterThanOrEqualTo(1).After(200, 10));
        }
    }
}