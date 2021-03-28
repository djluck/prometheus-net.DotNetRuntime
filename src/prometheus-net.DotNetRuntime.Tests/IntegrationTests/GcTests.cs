using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using NUnit.Framework;
using Prometheus.DotNetRuntime.Metrics;
using Prometheus.DotNetRuntime.Metrics.Producers;

namespace Prometheus.DotNetRuntime.Tests.IntegrationTests
{
    internal class Given_Only_Counters_Are_Available_For_GcStats : IntegrationTestBase<GcMetricsProducer>
    {
        protected override DotNetRuntimeStatsBuilder.Builder ConfigureBuilder(DotNetRuntimeStatsBuilder.Builder toConfigure)
        {
            return toConfigure.WithGcStats(CaptureLevel.Counters);
        }

        [Test]
        public void When_objects_are_allocated_then_the_allocated_bytes_counter_is_increased()
        {
            Assert.That(MetricProducer.AllocatedBytes.LabelNames, Is.Empty);
            var previousValue = MetricProducer.AllocatedBytes.Value;

            // allocate roughly 100kb+ of small objects
            for (int i = 0; i < 11; i++)
            {
                var b = new byte[10_000];
            }

            Assert.That(() => MetricProducer.AllocatedBytes.Value, Is.GreaterThanOrEqualTo(previousValue + 100_000).After(2_000, 10));
        }
        
        [Test]
        public void When_collections_happen_then_the_collection_count_is_increased([Values(0, 1, 2)] int generation)
        {
            Assert.That(MetricProducer.GcCollections.LabelNames, Is.EquivalentTo(new []{ "gc_generation" }));
            var previousValue = MetricProducer.GcCollections.Labels(generation.ToString()).Value;
            const int numCollectionsToRun = 10;
            
            // run collections
            for (int i = 0; i < numCollectionsToRun; i++)
            {
                GC.Collect(generation, GCCollectionMode.Forced);
            }

            Assert.That(() => MetricProducer.GcCollections.Labels(generation.ToString()).Value, Is.GreaterThanOrEqualTo(previousValue + numCollectionsToRun).After(2_000, 10));
        }
        
        [Test]
        public void When_a_garbage_collection_is_performed_then_the_heap_sizes_are_updated()
        {
            Assert.That(() => MetricProducer.GcHeapSizeBytes.Labels("0").Value, Is.GreaterThan(0).After(2000, 10));
            Assert.That(() => MetricProducer.GcHeapSizeBytes.Labels("1").Value, Is.GreaterThan(0).After(2000, 10));
            Assert.That(() => MetricProducer.GcHeapSizeBytes.Labels("2").Value, Is.GreaterThan(0).After(2000, 10));
            Assert.That(() => MetricProducer.GcHeapSizeBytes.Labels("loh").Value, Is.GreaterThan(0).After(2000, 10));
        }

        [Test]
        public void When_a_garbage_collection_is_performed_then_the_pause_ratios_can_be_calculated()
        {
            // arrange
            for (int i = 0; i < 5; i++)
                GC.Collect(2, GCCollectionMode.Forced, true, true);

            // assert
            Assert.That(() => MetricProducer.GcPauseRatio.Value, Is.GreaterThan(0.0).After(2000, 10));
        }
    }
     
    internal class Given_Gc_Info_Events_Are_Available_For_GcStats : IntegrationTestBase<GcMetricsProducer>
    {
        protected override DotNetRuntimeStatsBuilder.Builder ConfigureBuilder(DotNetRuntimeStatsBuilder.Builder toConfigure)
        {
            return toConfigure.WithGcStats(CaptureLevel.Informational);
        }

        [Test]
        public void When_objects_are_allocated_then_the_allocated_bytes_counter_is_increased()
        {
            Assert.That(MetricProducer.AllocatedBytes.LabelNames, Is.Empty);
            var previousValue = MetricProducer.AllocatedBytes.Value;

            // allocate roughly 100kb+ of small objects
            for (int i = 0; i < 11; i++)
            {
                var b = new byte[10_000];
            }

            Assert.That(() => MetricProducer.AllocatedBytes.Value, Is.GreaterThanOrEqualTo(previousValue + 100_000).After(2_000, 10));
        }
        
        [Test]
        public void When_a_garbage_collection_is_performed_then_the_heap_sizes_are_updated()
        {
            unsafe
            {
                // arrange (fix a variable to ensure the pinned objects counter is incremented
                var b = new byte[1];
                fixed (byte* p = b)
                {
                    // act
                    GC.Collect(0);
                }

                Assert.That(() => MetricProducer.GcHeapSizeBytes.Labels("0").Value, Is.GreaterThan(0).After(200, 10));
                Assert.That(() => MetricProducer.GcHeapSizeBytes.Labels("1").Value, Is.GreaterThan(0).After(200, 10));
                Assert.That(() => MetricProducer.GcHeapSizeBytes.Labels("2").Value, Is.GreaterThan(0).After(200, 10));
                Assert.That(() => MetricProducer.GcHeapSizeBytes.Labels("loh").Value, Is.GreaterThan(0).After(200, 10));
                Assert.That(() => MetricProducer.GcNumPinnedObjects.Value, Is.GreaterThan(0).After(200, 10));
            }
        }
        
        [Test]
        public void When_collections_happen_then_the_collection_count_is_increased([Values(0, 1, 2)] int generation)
        {
            double GetCollectionCount()
            {
                // Sum all the generation values (we cannot reliably know the reasons upfront)
                return MetricProducer.GcCollections.GetAllLabelValues()
                    .Where(l => l[0] == generation.ToString())
                    .Sum(l => MetricProducer.GcCollections.Labels(l).Value);
            }
            
            Assert.That(MetricProducer.GcCollections.LabelNames, Is.EquivalentTo(new []{ "gc_generation", "gc_reason" }));
            var previousValue = GetCollectionCount();
            const int numCollectionsToRun = 10;
            
            Thread.Sleep(2000);
            
            // run collections
            for (int i = 0; i < numCollectionsToRun; i++)
            {
                GC.Collect(generation, GCCollectionMode.Forced);
            }
            
            // assert
            
            // For some reason, the full number of gen0 collections are not being collected. I expect this is because .NET will not always force
            // a gen 0 collection to occur. 
            const int minExpectedCollections = numCollectionsToRun / 2;
            Assert.That(
                del: GetCollectionCount, 
                Is.GreaterThanOrEqualTo(previousValue + minExpectedCollections).After(2_000, 10)
            );
        }

        [Test]
        public void When_a_garbage_collection_is_performed_then_the_finalization_queue_is_updated()
        {
            // arrange
            {
                var finalizable = new FinalizableTest();
                finalizable = null;
            }
            GC.Collect(0);

            // assert
            Assert.That(() => MetricProducer.GcFinalizationQueueLength.Value, Is.GreaterThan(0).After(200, 10));
        }

        [Test]
        public void When_a_garbage_collection_is_performed_then_the_collection_and_pause_stats_and_reasons_are_updated()
        {
            // arrange
            GC.Collect(1, GCCollectionMode.Forced);
            GC.Collect(2, GCCollectionMode.Forced, true, true);

            // assert
            Assert.That(() => MetricProducer.GcCollectionSeconds.CollectAllCountValues().Count(), Is.GreaterThanOrEqualTo(1).After(500, 10)); // at least 3 generations
            Assert.That(() => MetricProducer.GcCollectionSeconds.CollectAllSumValues(excludeUnlabeled: true), Is.All.GreaterThan(0));
            Assert.That(() => MetricProducer.GcCollections.CollectAllValues(excludeUnlabeled: true), Is.All.GreaterThan(0));
            Assert.That(() => MetricProducer.GcPauseSeconds.CollectAllSumValues().Single(), Is.GreaterThan(0).After(500, 10));
        }

        [Test]
        public void When_a_garbage_collection_is_performed_then_the_gc_cpu_and_pause_ratios_can_be_calculated()
        {
            // arrange
            GC.Collect(2, GCCollectionMode.Forced, true, true);

            Assert.That(() => MetricProducer.GcPauseSeconds.CollectAllCountValues().First(), Is.GreaterThan(0).After(2000, 10));
            Assert.That(()=> MetricProducer.GcCollectionSeconds.CollectAllSumValues().Sum(x => x), Is.GreaterThan(0).After(2000, 10));
            
            // To improve the reliability of the test, do some CPU busy work + call UpdateMetrics here.
            // Why? Process.TotalProcessorTime isn't very precise (it's not updated after every small bit of CPU consumption)
            // and this can lead to CpuRatio believing that no CPU has been consumed
            long i = 2_000_000_000;
            while (i > 0)
                i--;

            // act 
            MetricProducer.UpdateMetrics();
            
            // assert
            Assert.That(MetricProducer.GcPauseRatio.Value, Is.GreaterThan(0.0).After(1000, 1), "GcPauseRatio");
            Assert.That(MetricProducer.GcCpuRatio.Value, Is.GreaterThan(0.0).After(1000, 1), "GcCpuRatio");
        }

        public class FinalizableTest
        {
            ~FinalizableTest()
            {
                // Sleep for a bit so our object won't exit the finalization queue immediately
                Thread.Sleep(1000);
            }
        }
    }

    internal class Given_Gc_Verbose_Events_Are_Available_For_GcStats : IntegrationTestBase<GcMetricsProducer>
    {
        protected override DotNetRuntimeStatsBuilder.Builder ConfigureBuilder(DotNetRuntimeStatsBuilder.Builder toConfigure)
        {
            return toConfigure.WithGcStats(CaptureLevel.Verbose);
        }

        [Test]
        public void When_100kb_of_small_objects_are_allocated_then_the_allocated_bytes_counter_is_increased()
        {
            var previousValue = MetricProducer.AllocatedBytes.Labels("soh").Value;

            // allocate roughly 100kb+ of small objects
            for (int i = 0; i < 11; i++)
            {
                var b = new byte[10_000];
            }

            Assert.That(() => MetricProducer.AllocatedBytes.Labels("soh").Value, Is.GreaterThanOrEqualTo(previousValue + 100_000).After(500, 10));
        }

        [Test]
        public void When_a_100kb_large_object_is_allocated_then_the_allocated_bytes_counter_is_increased()
        {
            var previousValue = MetricProducer.AllocatedBytes.Labels("loh").Value;

            // allocate roughly 100kb+ of large objects
            var b = new byte[110_000];

            Assert.That(() => MetricProducer.AllocatedBytes.Labels("loh").Value, Is.GreaterThanOrEqualTo(previousValue + 100_000).After(500, 10));
        }
    }
}