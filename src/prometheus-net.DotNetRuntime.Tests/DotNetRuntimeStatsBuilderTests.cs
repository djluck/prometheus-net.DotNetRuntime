using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
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
            {
                await Assert_Expected_Stats_Are_Present_In_Registry(GetDefaultRegistry());
            }
        }

        [Test]
        public async Task Default_registers_all_expected_stats_to_a_custom_registry()
        {
            // arrange
            var registry = NewRegistry();
            using (DotNetRuntimeStatsBuilder.Default().StartCollecting(registry))
            {
                await Assert_Expected_Stats_Are_Present_In_Registry(registry);
            }
        }
        
        [Test]
        public void WithCustomCollector_will_not_register_the_same_collector_twice()
        {
            var expectedCollector = new GcStatsCollector();
            var builder = DotNetRuntimeStatsBuilder
                .Customize()
                .WithGcStats()
                .WithCustomCollector(expectedCollector);

            Assert.That(builder.StatsCollectors.Count, Is.EqualTo(1));
            Assert.That(builder.StatsCollectors.TryGetValue(new GcStatsCollector(), out var actualColector), Is.True);
            Assert.That(actualColector, Is.SameAs(expectedCollector));
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
        public async Task StartCollecting_Allows_A_New_Collector_To_Run_After_Disposing_A_Previous_Collector()
        {
            using (DotNetRuntimeStatsBuilder.Customize().StartCollecting())
            {
                await Assert_Expected_Stats_Are_Present_In_Registry(GetDefaultRegistry());
            }

            using (DotNetRuntimeStatsBuilder.Customize().StartCollecting())
            {
                await Assert_Expected_Stats_Are_Present_In_Registry(GetDefaultRegistry());
            }
        }

        [Test]
        public void StartCollecting_Does_Not_Allow_Two_Collectors_To_Run_Simultaneously_For_Each_Registry_Instance()
        {
            var registry1 = NewRegistry();;
            var registry2 = NewRegistry();;

            using (DotNetRuntimeStatsBuilder.Customize().StartCollecting())
            {
                Assert.Throws<InvalidOperationException>(() => DotNetRuntimeStatsBuilder.Customize().StartCollecting());
                using (DotNetRuntimeStatsBuilder.Customize().StartCollecting(registry1))
                {
                    Assert.Throws<InvalidOperationException>(() => DotNetRuntimeStatsBuilder.Customize().StartCollecting());
                    Assert.Throws<InvalidOperationException>(() => DotNetRuntimeStatsBuilder.Customize().StartCollecting(registry1));
                    using (DotNetRuntimeStatsBuilder.Customize().StartCollecting(registry2))
                    {
                        Assert.Throws<InvalidOperationException>(() => DotNetRuntimeStatsBuilder.Customize().StartCollecting());
                        Assert.Throws<InvalidOperationException>(() => DotNetRuntimeStatsBuilder.Customize().StartCollecting(registry1));
                        Assert.Throws<InvalidOperationException>(() => DotNetRuntimeStatsBuilder.Customize().StartCollecting(registry2));
                    }

                    Assert.Throws<InvalidOperationException>(() => DotNetRuntimeStatsBuilder.Customize().StartCollecting());
                    Assert.Throws<InvalidOperationException>(() => DotNetRuntimeStatsBuilder.Customize().StartCollecting(registry1));
                }

                Assert.Throws<InvalidOperationException>(() => DotNetRuntimeStatsBuilder.Customize().StartCollecting());
            }
        }

        [Test]
        public void StartCollecting_Allows_A_New_Collector_To_Run_After_Disposing_Previous_Collector_For_Each_Registry_Instance()
        {
            var registry1 = NewRegistry();
            var registry2 = NewRegistry();

            using (DotNetRuntimeStatsBuilder.Customize().StartCollecting(registry1))
            {
                using (DotNetRuntimeStatsBuilder.Customize().StartCollecting(registry2))
                {
                }

                using (DotNetRuntimeStatsBuilder.Customize().StartCollecting(registry2))
                {
                }
            }

            using (DotNetRuntimeStatsBuilder.Customize().StartCollecting(registry2))
            {
                using (DotNetRuntimeStatsBuilder.Customize().StartCollecting(registry1))
                {
                }

                using (DotNetRuntimeStatsBuilder.Customize().StartCollecting(registry1))
                {
                }
            }
        }

        private async Task Assert_Expected_Stats_Are_Present_In_Registry(
#if PROMV2
            DefaultCollectorRegistry registry
#else
            CollectorRegistry registry
#endif
        )
        {
            // arrange
            const int metricsPort = 12203;
            using (var metricServer = new MetricServer(metricsPort, registry: registry))
            using (var client = new HttpClient() { Timeout = TimeSpan.FromSeconds(2) })
            {
                metricServer.Start();

                // act + assert
                using (var resp = await client.GetAsync($"http://localhost:{metricsPort}/metrics"))
                {
                    var content = await resp.Content.ReadAsStringAsync();

                    // Some basic assertions to check that the output of our stats collectors is present
                    Assert.That(content, Contains.Substring("dotnet_threadpool"));
                    Assert.That(content, Contains.Substring("dotnet_jit"));
                    Assert.That(content, Contains.Substring("dotnet_gc"));
                    Assert.That(content, Contains.Substring("dotnet_contention"));
                    Assert.That(content, Contains.Substring("dotnet_build_info"));
                    Assert.That(content, Contains.Substring("process_cpu_count"));
                }
            }
        }
        
#if PROMV2
        private DefaultCollectorRegistry NewRegistry()
        {
            return new DefaultCollectorRegistry();
        }

        private DefaultCollectorRegistry GetDefaultRegistry()
        {
            return DefaultCollectorRegistry.Instance;
        }
        
#elif PROMV3
        private CollectorRegistry NewRegistry()
        {
            return Metrics.NewCustomRegistry();
        }

        private CollectorRegistry GetDefaultRegistry()
        {
            return Metrics.DefaultRegistry;
        }
#endif
        
    }
}