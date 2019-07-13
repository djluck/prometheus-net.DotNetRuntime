using System;
using System.Threading;
using System.Threading.Tasks;
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
            
            // wait for event listener thread to spin up
            while (!_eventListener.StartedReceivingEvents)
            {
                Thread.Sleep(10); 
                Console.Write("Waiting.. ");
                
            }
            Console.WriteLine("EventListener should be active now.");
        }

        [TearDown]
        public void TearDown()
        {
            _eventListener.Dispose();
        }

        protected abstract TStatsCollector CreateStatsCollector();
    }
}