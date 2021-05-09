using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Prometheus.DotNetRuntime.EventListening.Parsers;

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
                await Assert_Expected_Stats_Are_Present_In_Registry(Prometheus.Metrics.DefaultRegistry);
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
            var registry = NewRegistry();
            using (DotNetRuntimeStatsBuilder.Default().StartCollecting(registry))
            {
                await Assert_Expected_Stats_Are_Present_In_Registry(registry);
            }

            using (DotNetRuntimeStatsBuilder.Default().StartCollecting(registry))
            {
                await Assert_Expected_Stats_Are_Present_In_Registry(registry);
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

        [Test]
        public void RegisterDefaultConsumers_Can_Register_Default_Consumers_For_All_Parsers()
        {
            // arrange
            var services = new ServiceCollection();
            
            // act
            DotNetRuntimeStatsBuilder.Builder.RegisterDefaultConsumers(services);
            var sp = services.BuildServiceProvider();
            
            // assert
            var infoConsumer = sp.GetService<Consumes<GcEventParser.Events.Info>>();
            Assert.That(infoConsumer.Enabled, Is.False);
            Assert.That(infoConsumer.Events, Is.Null);

            Assert.That(sp.GetService<Consumes<GcEventParser.Events.Verbose>>, Is.Not.Null);
            Assert.That(sp.GetService<Consumes<ExceptionEventParser.Events.Error>>, Is.Not.Null);
            Assert.That(sp.GetService<Consumes<RuntimeEventParser.Events.CountersV3_0>>, Is.Not.Null);
        }

        [Test]
        public void Cannot_Register_Tasks_At_Unsupported_Levels()
        {
            var ex = Assert.Throws<UnsupportedCaptureLevelException>(() => DotNetRuntimeStatsBuilder.Customize().WithGcStats(CaptureLevel.Errors));
            Assert.That(ex.SpecifiedLevel, Is.EqualTo(CaptureLevel.Errors));
            Assert.That(ex.SupportedLevels, Is.EquivalentTo(new []{ CaptureLevel.Verbose, CaptureLevel.Informational}));

            ex = Assert.Throws<UnsupportedCaptureLevelException>(() => DotNetRuntimeStatsBuilder.Customize().WithThreadPoolStats(CaptureLevel.Verbose));
            Assert.That(ex.SpecifiedLevel, Is.EqualTo(CaptureLevel.Verbose));
            Assert.That(ex.SupportedLevels, Is.EquivalentTo(new []{ CaptureLevel.Counters, CaptureLevel.Informational}));
        }
        
        [Test]
        public async Task Debugging_Metrics_Works_Correctly()
        {
            // arrange
            var registry = NewRegistry();
            
            using (DotNetRuntimeStatsBuilder.Default().WithDebuggingMetrics(true).StartCollecting(registry))
            {
                await Assert_Expected_Stats_Are_Present_In_Registry(registry, shouldContainDebug: true);
            }
        }
        
        private async Task Assert_Expected_Stats_Are_Present_In_Registry(
            CollectorRegistry registry,
            bool shouldContainDebug = false
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
                    Assert.That(content, Contains.Substring("dotnet_gc"));
                    Assert.That(content, Contains.Substring("dotnet_contention"));
                    Assert.That(content, Contains.Substring("dotnet_build_info"));
                    Assert.That(content, Contains.Substring("process_cpu_count"));

                    if (shouldContainDebug)
                    {
                        Assert.That(content, Contains.Substring("dotnet_debug_event"));
                    }
                    else
                        StringAssert.DoesNotContain("dotnet_debug", content);
                }
            }
        }

        private CollectorRegistry NewRegistry()
        {
            return Prometheus.Metrics.NewCustomRegistry();
        }
    }
}