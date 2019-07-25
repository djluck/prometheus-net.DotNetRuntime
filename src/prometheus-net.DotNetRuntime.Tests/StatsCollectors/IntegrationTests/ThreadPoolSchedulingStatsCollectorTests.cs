using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Prometheus.DotNetRuntime.StatsCollectors;
using Prometheus.DotNetRuntime.StatsCollectors.Util;

namespace Prometheus.DotNetRuntime.Tests.StatsCollectors.IntegrationTests
{
    [TestFixture]
    internal class Given_A_ThreadPoolSchedulingStatsCollector_That_Samples_Every_Event : StatsCollectorIntegrationTestBase<ThreadPoolSchedulingStatsCollector>
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
            // First call to Task.Run triggers a queued event but not a queue event. For now, call twice 
            await Task.Run(() => 1 );
            var sp = Stopwatch.StartNew();
            await Task.Run(() => sp.Stop());
            sp.Stop();
            
            Assert.That(() => StatsCollector.ScheduledCount.Value, Is.GreaterThanOrEqualTo(1).After(100, 10));
            Assert.That(StatsCollector.ScheduleDelay.CollectAllCountValues().Single(), Is.GreaterThanOrEqualTo(1));
            Assert.That(StatsCollector.ScheduleDelay.CollectAllSumValues().Single(), Is.EqualTo(sp.Elapsed.TotalSeconds).Within(0.01));
        }
    }
    
    [TestFixture]
    internal class Given_A_ThreadPoolSchedulingStatsCollector_That_Samples_Fifth_Event : StatsCollectorIntegrationTestBase<ThreadPoolSchedulingStatsCollector>
    {
        protected override ThreadPoolSchedulingStatsCollector CreateStatsCollector()
        {
            return new ThreadPoolSchedulingStatsCollector(Constants.DefaultHistogramBuckets, SampleEvery.FiveEvents);
        }

        [Test]
        public async Task When_many_items_of_work_is_queued_on_the_thread_pool_then_the_queued_and_scheduled_work_is_measured()
        {
            Assert.That(StatsCollector.ScheduledCount.Value, Is.EqualTo(0));
            
            // act (Task.Run will execute the function on the thread pool)
            // There seems to be either a bug in the implementation of .NET core or a bug in my understanding...
            // First call to Task.Run triggers a queued event but not a queue event. For now, call twice 
            await Task.Run(() => 1 );

            var sp = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++)
            {
                sp.Start();
                await Task.Run(() => sp.Stop());
            }

            Assert.That(() => StatsCollector.ScheduledCount.Value, Is.GreaterThanOrEqualTo(100).After(100, 10));
            Assert.That(StatsCollector.ScheduleDelay.CollectAllCountValues().Single(), Is.GreaterThanOrEqualTo(100));
            Assert.That(StatsCollector.ScheduleDelay.CollectAllSumValues().Single(), Is.EqualTo(sp.Elapsed.TotalSeconds).Within(0.01));
        }
    }
}