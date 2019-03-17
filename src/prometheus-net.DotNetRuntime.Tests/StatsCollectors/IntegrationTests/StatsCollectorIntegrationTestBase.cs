using System.Threading;
using NUnit.Framework;
#if PROMV2
using Prometheus.Advanced;    
#endif
using Prometheus.DotNetRuntime;
using Prometheus.DotNetRuntime.StatsCollectors;

namespace Prometheus.DotNetRuntime.Tests.StatsCollectors.IntegrationTests
{
    [TestFixture]
    internal abstract class StatsCollectorIntegrationTestBase<TStatsCollector> 
        where TStatsCollector : IEventSourceStatsCollector
    {
        private DotNetEventListener _eventListener;
        protected TStatsCollector StatsCollector { get; private set; }

        [SetUp]
        public void SetUp()
        {
            StatsCollector = CreateStatsCollector();
#if PROMV2
            StatsCollector.RegisterMetrics(new MetricFactory(new DefaultCollectorRegistry()));
#elif PROMV3
            StatsCollector.RegisterMetrics(Metrics.WithCustomRegistry(Metrics.NewCustomRegistry()));
#endif
            _eventListener = new DotNetEventListener(StatsCollector, null);
            Thread.Sleep(100); // wait for event listener thread to spin up
        }

        [TearDown]
        public void TearDown()
        {
            _eventListener.Dispose();
        }

        protected abstract TStatsCollector CreateStatsCollector();
    }
}