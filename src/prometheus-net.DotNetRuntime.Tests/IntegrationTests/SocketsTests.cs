using NUnit.Framework;
using Prometheus.DotNetRuntime.Metrics.Producers;

namespace Prometheus.DotNetRuntime.Tests.IntegrationTests
{
    // TODO need to test incoming network activity
    public class SocketsTests : IntegrationTestBase<SocketsMetricProducer>
    {
        [Test]
        public void Given_No_Network_Activity_Then_Metrics_Should_Not_Increase()
        {
            
        }

        [Test]
        public void Given_A_HTTP_Request_Then_Outgoing_metrics_Should_Increase()
        {
            
        }

        protected override DotNetRuntimeStatsBuilder.Builder ConfigureBuilder(DotNetRuntimeStatsBuilder.Builder toConfigure)
        {
            return toConfigure.WithSocketStats();
        }
    }
}