using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework;
using Prometheus.DotNetRuntime.Metrics.Producers;

namespace Prometheus.DotNetRuntime.Tests.IntegrationTests
{
#if !NETCOREAPP3_1
    // TODO need to test incoming network activity
    public class SocketsTests : IntegrationTestBase<SocketsMetricProducer>
    {
        [Test]
        public void Given_No_Network_Activity_Then_Metrics_Should_Not_Increase()
        {
            Assert.That(MetricProducer.BytesReceived.Value, Is.Zero);
            Assert.That(MetricProducer.BytesSent.Value, Is.Zero);
            Assert.That(MetricProducer.IncomingConnectionEstablished.Value, Is.Zero);
            Assert.That(MetricProducer.OutgoingConnectionEstablished.Value, Is.Zero);
        }

        [Test]
        public async Task Given_A_HTTP_Request_Then_Outgoing_metrics_Should_Increase()
        {
            // arrange
            using var client = new HttpClient(new SocketsHttpHandler()
            {
                PooledConnectionLifetime = TimeSpan.MaxValue,
                MaxConnectionsPerServer = 10
            });

            // act
            var requests = Enumerable.Range(1, 20)
                .Select(n => client.GetAsync("https://httpstat.us/200?sleep=3000"))
                .ToArray();

            // assert
            Assert.That(() => MetricProducer.OutgoingConnectionEstablished.Value, Is.GreaterThanOrEqualTo(10).After(2_000, 100));
            Assert.That(MetricProducer.BytesSent.Value, Is.GreaterThan(0));
            
            await Task.WhenAll(requests);
            client.Dispose();
        }

        protected override DotNetRuntimeStatsBuilder.Builder ConfigureBuilder(DotNetRuntimeStatsBuilder.Builder toConfigure)
        {
            return toConfigure.WithSocketStats();
        }
    }
#endif
}