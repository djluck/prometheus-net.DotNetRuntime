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
    public abstract class IntegrationTestBase<TMetricProducer>
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
            var waitFor = TimeSpan.FromSeconds(10);

            Console.Write("Waiting for event listeners to be active.. ");
            if (!SpinWait.SpinUntil(() =>
                    _collector.EventListeners.All(x => x.StartedReceivingEvents),
                    waitFor))
            {
                var notReadySources =
                    _collector.EventListeners.Where(x => !x.StartedReceivingEvents)
                        .Select(x => x.EventListener.EventSourceName)
                        .ToList();

                if (notReadySources.Any())
                    Assert.Fail($"Waited {waitFor} and still not all event listeners were ready! Event listeners not ready: {string.Join(", ", notReadySources)}");
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
