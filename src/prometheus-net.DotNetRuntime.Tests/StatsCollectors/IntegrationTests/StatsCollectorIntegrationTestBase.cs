using System.Threading;
using NUnit.Framework;
using Prometheus.Advanced;
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
            StatsCollector.RegisterMetrics(new DefaultCollectorRegistry());
            _eventListener = new DotNetEventListener(StatsCollector);
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