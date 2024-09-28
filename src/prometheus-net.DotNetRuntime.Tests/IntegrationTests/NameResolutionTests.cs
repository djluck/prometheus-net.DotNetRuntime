using System.Linq;
using System.Net;
using System.Threading.Tasks;
using NUnit.Framework;
using Prometheus.DotNetRuntime.Metrics.Producers;

namespace Prometheus.DotNetRuntime.Tests.IntegrationTests
{
    public class NameResolutionTests : IntegrationTestBase<NameResolutionMetricProducer>
    {
        [Test]
        public async Task Given_a_DNS_lookup_metrics_should_increase()
        {
            // arrange
            var initialLookups = MetricProducer.DnsLookups.Value;
            var initialLookupDuration = MetricProducer.DnsLookupDuration.Sum;

            // act
            var lookups = Enumerable.Range(1, 10)
                .Select(n => Dns.GetHostEntryAsync("localhost"))
                .ToArray();

            await Task.WhenAll(lookups);

            // assert
            Assert.That(() => MetricProducer.DnsLookups.Value, Is.GreaterThanOrEqualTo(initialLookups + 10).After(10_000, 100));
            Assert.That(MetricProducer.DnsLookupDuration.Sum, Is.GreaterThan(initialLookupDuration));
        }

        protected override DotNetRuntimeStatsBuilder.Builder ConfigureBuilder(DotNetRuntimeStatsBuilder.Builder toConfigure)
        {
            // Do a DNS lookup to force creation of the NameResolution event source.
            Dns.GetHostEntry("localhost");

            return toConfigure.WithNameResolution();
        }
    }
}
