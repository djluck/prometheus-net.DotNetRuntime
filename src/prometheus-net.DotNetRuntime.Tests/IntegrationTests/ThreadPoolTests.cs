using System;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Prometheus.DotNetRuntime.Metrics;
using Prometheus.DotNetRuntime.Metrics.Producers;

namespace Prometheus.DotNetRuntime.Tests.IntegrationTests
{
    internal class Given_Only_Runtime_Counters_Are_Enabled_For_ThreadPoolStats : IntegrationTestBase<ThreadPoolMetricsProducer>
    {
        protected override DotNetRuntimeStatsBuilder.Builder ConfigureBuilder(DotNetRuntimeStatsBuilder.Builder toConfigure)
        {
            return toConfigure.WithThreadPoolStats(CaptureLevel.Counters);
        }
        
        [Test]
        public async Task When_work_is_executed_on_the_thread_pool_then_executed_work_is_measured()
        {
            var startingThroughput = MetricProducer.Throughput.Value;
            const int numTasksToSchedule = 100;
            // schedule a bunch of tasks
            var tasks = Enumerable.Range(1, numTasksToSchedule)
                .Select(_ => Task.Run(() => { }));

            await Task.WhenAll(tasks);
            
            Assert.That(() => MetricProducer.NumThreads.Value, Is.GreaterThanOrEqualTo(Environment.ProcessorCount).After(2_000, 10));
            Assert.That(() => MetricProducer.Throughput.Value, Is.GreaterThanOrEqualTo(startingThroughput + numTasksToSchedule).After(2_000, 10));
        }
        
        [Test]
        public async Task When_timers_are_active_then_they_are_measured()
        {
            var startingTimers = MetricProducer.NumTimers.Value;
            const int numTimersToSchedule = 100;
            // schedule a bunch of timers
            var tasks = Enumerable.Range(1, numTimersToSchedule)
                .Select(n => Task.Delay(3000 + n))
                .ToArray();

            Assert.That(() => MetricProducer.NumTimers.Value, Is.GreaterThanOrEqualTo(startingTimers + numTimersToSchedule).After(2_000, 10));
        }
        
        [Test]
        public async Task When_blocking_work_is_executed_on_the_thread_pool_then_thread_pool_delays_are_measured()
        {
            var startingQueueLength = MetricProducer.QueueLength.Sum;
            var sleepDelay = TimeSpan.FromMilliseconds(250);
            int desiredSecondsToBlock = 5;
            int numTasksToSchedule = (int)(Environment.ProcessorCount / sleepDelay.TotalSeconds) * desiredSecondsToBlock;
            
            Console.WriteLine($"Scheduling {numTasksToSchedule} blocking tasks...");
            // schedule a bunch of blocking tasks that will make the thread pool will grow
            var tasks = Enumerable.Range(1, numTasksToSchedule)
                .Select(_ => Task.Run(() => Thread.Sleep(sleepDelay)))
                .ToArray();

            // dont' wait for the tasks to complete- we want to see stats present during a period of thread pool starvation
            
            Assert.That(() => MetricProducer.NumThreads.Value, Is.GreaterThan(Environment.ProcessorCount).After(desiredSecondsToBlock * 1000, 10));
            Assert.That(() => MetricProducer.QueueLength.Sum, Is.GreaterThan(startingQueueLength).After(desiredSecondsToBlock * 1000, 10));
        }
    }
    
    internal class Given_Runtime_Counters_And_ThreadPool_Info_Events_Are_Enabled_For_ThreadPoolStats : IntegrationTestBase<ThreadPoolMetricsProducer>
    {
        protected override DotNetRuntimeStatsBuilder.Builder ConfigureBuilder(DotNetRuntimeStatsBuilder.Builder toConfigure)
        {
            return toConfigure.WithThreadPoolStats(CaptureLevel.Informational);
        }
        
        [Test]
        public async Task When_work_is_executed_on_the_thread_pool_then_executed_work_is_measured()
        {
            // schedule a bunch of blocking tasks that will make the thread pool will grow
            var tasks = Enumerable.Range(1, 1000)
                .Select(_ => Task.Run(() => Thread.Sleep(20)));

            await Task.WhenAll(tasks);

            Assert.That(() => MetricProducer.NumThreads.Value, Is.GreaterThanOrEqualTo(Environment.ProcessorCount).After(2000, 10));
            Assert.That(MetricProducer.AdjustmentsTotal.CollectAllValues().Sum(), Is.GreaterThanOrEqualTo(1));
        }
        
        [Test]
        public async Task When_IO_work_is_executed_on_the_thread_pool_then_the_number_of_io_threads_is_measured()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Assert.Inconclusive("Cannot run this test on non-windows platforms.");
            
            // need to schedule a bunch of IO work to make the IO pool grow
            using (var client = new HttpClient())
            {
                var httpTasks = Enumerable.Range(1, 50)
                    .Select(_ => client.GetAsync("http://google.com"));

                await Task.WhenAll(httpTasks);
            }
          
            Assert.That(() => MetricProducer.NumIocThreads.Value, Is.GreaterThanOrEqualTo(1).After(2000, 10));
        }
    }
}