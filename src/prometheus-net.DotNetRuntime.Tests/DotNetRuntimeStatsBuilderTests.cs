using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework;
#if PROMV2
using Prometheus.Advanced;    
#endif
using Prometheus.DotNetRuntime.StatsCollectors;

namespace Prometheus.DotNetRuntime.Tests
{
    [TestFixture]
    public class DotNetRuntimeStatsBuilderTests
    {
        /// <summary>
        /// Verifies that the default stats collectors can be registered with prometheus and that their metrics
        /// are being outputted to the metric server. 
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task Default_registers_all_expected_stats()
        {
            // arrange
            using (DotNetRuntimeStatsBuilder.Default().StartCollecting())
            using (var metricServer = new MetricServer(12203))
            using (var client = new HttpClient())
            {
                metricServer.Start();

                // act + assert
                using (var resp = await client.GetAsync("http://localhost:12203/metrics"))
                {
                    var content = await resp.Content.ReadAsStringAsync();
                    
                    // Some basic assertions to check that the output of our stats collectors is present
                    Assert.That(content, Contains.Substring("dotnet_threadpool"));
                    Assert.That(content, Contains.Substring("dotnet_jit"));
                    Assert.That(content, Contains.Substring("dotnet_gc"));
                    Assert.That(content, Contains.Substring("dotnet_contention"));
                }
            }
        }

        [Test]
        public void WithCustomCollector_will_not_register_the_same_collector_twice()
        {
            var builder = DotNetRuntimeStatsBuilder
                .Customize()
                .WithGcStats()
                .WithCustomCollector(new GcStatsCollector());

            Assert.That(builder.StatsCollectors.Count, Is.EqualTo(1));
        }
        
        [Test]
        public void StartCollecting_Does_Not_Allow_Two_Collectors_To_Run_Simultaneously()
        {
            using (DotNetRuntimeStatsBuilder.Customize().StartCollecting())
            {
                Assert.Throws<InvalidOperationException>(() => DotNetRuntimeStatsBuilder.Customize().StartCollecting());
            }
        }
        
        [Test]
        public void StartCollecting_Allows_A_New_Collector_To_Run_After_Disposing_A_Previous_Collector()
        {
            using (DotNetRuntimeStatsBuilder.Customize().StartCollecting())
            {
            }
            
            using (DotNetRuntimeStatsBuilder.Customize().StartCollecting())
            {
            }
        }
    }
}