using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Prometheus.DotNetRuntime.EventListening;
using Prometheus.DotNetRuntime.EventListening.Parsers;

namespace Prometheus.DotNetRuntime.Tests.EventListening
{
    [TestFixture]
    public class DotNetRuntimeStatsCollectorTests
    {
        [Test]
        [Timeout(20_000)]
        public async Task After_Recycling_Then_Events_Can_Still_Be_Processed_Correctly()
        {
            // arrange
            var parser = new RuntimeEventParser() { RefreshIntervalSeconds = 1};
            var eventAssertion = TestHelpers.ArrangeEventAssertion<IncrementingCounterValue>(e => parser.ExceptionCount += e);
            var services = new ServiceCollection();
            var parserRego = ListenerRegistration.Create(CaptureLevel.Counters, _ => parser);
            parserRego.RegisterServices(services);
            services.AddSingleton<ISet<ListenerRegistration>, HashSet<ListenerRegistration>>(_ => new[] { parserRego }.ToHashSet());
            
            // act
            using var l = new DotNetRuntimeStatsCollector(services.BuildServiceProvider(), new CollectorRegistry(), new DotNetRuntimeStatsCollector.Options() { RecycleListenersEvery = TimeSpan.FromSeconds(3)});
            Assert.That(() => eventAssertion.Fired, Is.True.After(2000, 10));
            await Task.Delay(TimeSpan.FromSeconds(10));

            // Why do we expected this value of events? Although we are waiting for 10 seconds for events, recycles may cause a counter period
            // to not fire. As counter events fire each second, as long as this value is greater than the recycle period this test can veryify
            // recycling is working correctly.
            const int expectedCounterEvents = 6;
            Assert.That(eventAssertion.History.Count, Is.GreaterThanOrEqualTo(expectedCounterEvents));
            Assert.That(l.EventListenerRecycles.Value, Is.InRange(3, 5));
        }
    }
}