using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Prometheus.DotNetRuntime.Metrics;

namespace Prometheus.DotNetRuntime.Tests.IntegrationTests
{
    [TestFixture]
    internal abstract class IntegrationTestBase<TMetricProducer>
        where TMetricProducer : IMetricProducer
    {
        private DotNetRuntimeStatsCollector _collector;
        protected TMetricProducer MetricProducer { get; private set; }

        [SetUp]
        public void SetUp()
        {
            _collector = (DotNetRuntimeStatsCollector) ConfigureBuilder(DotNetRuntimeStatsBuilder.Customize())
                .StartCollecting(Prometheus.Metrics.NewCustomRegistry());
            
            MetricProducer = (TMetricProducer)_collector.ServiceProvider.GetServices<IMetricProducer>().Single(x => x is TMetricProducer);

            // wait for event listener thread to spin up
            var waitingFor = Stopwatch.StartNew();
            var waitFor = TimeSpan.FromSeconds(10);
            
            while (!_collector.EventListeners.All(x => x.StartedReceivingEvents))
            {
                Thread.Sleep(10); 
                Console.Write("Waiting for event listeners to be active.. ");
                
                if (waitingFor.Elapsed > waitFor)
                {
                    Assert.Fail($"Waited {waitFor} and still not all event listeners were ready! Event listeners not ready: {string.Join(", ", _collector.EventListeners.Where(x => !x.StartedReceivingEvents))}");
                    return;
                }
            }
            
            Console.WriteLine("All event listeners should be active now.");
        }

        [TearDown]
        public void TearDown()
        {
            _collector.Dispose();
        }

        protected abstract DotNetRuntimeStatsBuilder.Builder ConfigureBuilder(DotNetRuntimeStatsBuilder.Builder toConfigure);
    }
}