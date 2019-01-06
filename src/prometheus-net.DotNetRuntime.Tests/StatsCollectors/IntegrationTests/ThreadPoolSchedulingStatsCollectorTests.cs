using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Prometheus.Advanced;
using Prometheus.DotNetRuntime;
using Prometheus.DotNetRuntime.StatsCollectors;

namespace Prometheus.DotNetRuntime.Tests.StatsCollectors.IntegrationTests
{
    [TestFixture]
    internal class ThreadPoolSchedulingStatsCollectorTests : StatsCollectorIntegrationTestBase<ThreadPoolSchedulingStatsCollector>
    {
        protected override ThreadPoolSchedulingStatsCollector CreateStatsCollector()
        {
            return new ThreadPoolSchedulingStatsCollector();
        }

        [Test]
        [Repeat(5)]
        public async Task When_work_is_queued_on_the_thread_pool_then_the_queued_and_scheduled_work_is_measured()
        {
            Assert.That(StatsCollector.ScheduledCount.Value, Is.EqualTo(0));
            
            // act (Task.Run will execute the function on the thread pool)
            // There seems to be either a bug in the implementation of .NET core or a bug in my understanding...
            // First call to Task.Run triggers a qequeued event but not a queue event. For now, call twice 
            await Task.Run(() => 1 );
            var sp = Stopwatch.StartNew();
            await Task.Run(() => sp.Stop() );
            
            Assert.That(() => StatsCollector.ScheduledCount.Value, Is.GreaterThanOrEqualTo(1).After(100, 10));
            var histogramValue = StatsCollector.ScheduleDelay.CollectSingle().First().histogram;
            
            Assert.That(histogramValue.sample_count, Is.GreaterThanOrEqualTo(1));
            Assert.That(histogramValue.sample_sum, Is.EqualTo(sp.Elapsed.TotalSeconds).Within(0.01));
        }
    }
}