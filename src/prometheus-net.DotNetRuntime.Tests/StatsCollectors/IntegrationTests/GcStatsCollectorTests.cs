using System;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Prometheus.DotNetRuntime.StatsCollectors;

namespace Prometheus.DotNetRuntime.Tests.StatsCollectors.IntegrationTests
{
    internal class GcStatsCollectorTests : StatsCollectorIntegrationTestBase<GcStatsCollector>
    {
        protected override GcStatsCollector CreateStatsCollector()
        {
            return new GcStatsCollector();
        }

        [Test]
        public void When_100kb_of_small_objects_are_allocated_then_the_allocated_bytes_counter_is_increased()
        {
            var previousValue = StatsCollector.AllocatedBytes.Labels("soh").Value;

            // allocate roughly 100kb+ of small objects
            for (int i = 0; i < 11; i++)
            {
                var b = new byte[10_000];
            }

            Assert.That(() => StatsCollector.AllocatedBytes.Labels("soh").Value, Is.GreaterThanOrEqualTo(previousValue + 100_000).After(500, 10));
        }

        [Test]
        public void When_a_100kb_large_object_is_allocated_then_the_allocated_bytes_counter_is_increased()
        {
            var previousValue = StatsCollector.AllocatedBytes.Labels("loh").Value;

            // allocate roughly 100kb+ of large objects
            var b = new byte[110_000];

            Assert.That(() => StatsCollector.AllocatedBytes.Labels("loh").Value, Is.GreaterThanOrEqualTo(previousValue + 100_000).After(500, 10));
        }

        [Test]
        public void When_a_garbage_collection_is_performed_then_the_heap_sizes_are_updated()
        {
            GC.Collect(0);

            Assert.That(() => StatsCollector.GcHeapSizeBytes.Labels("0").Value, Is.GreaterThan(0).After(200, 10));
            Assert.That(() => StatsCollector.GcHeapSizeBytes.Labels("1").Value, Is.GreaterThan(0).After(200, 10));
            Assert.That(() => StatsCollector.GcHeapSizeBytes.Labels("2").Value, Is.GreaterThan(0).After(200, 10));
            Assert.That(() => StatsCollector.GcHeapSizeBytes.Labels("loh").Value, Is.GreaterThan(0).After(200, 10));
            Assert.That(() => StatsCollector.GcNumPinnedObjects.Value, Is.GreaterThan(0).After(200, 10));
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
            Assert.That(() => StatsCollector.GcFinalizationQueueLength.Value, Is.GreaterThan(0).After(200, 10));
        }


        [Test]
        public void When_a_garbage_collection_is_performed_then_the_collection_and_pause_stats_and_reasons_are_updated()
        {
            // arrange
            GC.Collect(1, GCCollectionMode.Forced);
            GC.Collect(2, GCCollectionMode.Forced, true, true);

            // assert
            Assert.That(() => StatsCollector.GcCollectionSeconds.CollectAllCountValues().Count(), Is.GreaterThanOrEqualTo(1).After(500, 10)); // at least 3 generations
            Assert.That(() => StatsCollector.GcCollectionSeconds.CollectAllSumValues(excludeUnlabeled: true), Is.All.GreaterThan(0));
            Assert.That(() => StatsCollector.GcCollectionReasons.CollectAllValues(excludeUnlabeled: true), Is.All.GreaterThan(0));
            Assert.That(() => StatsCollector.GcPauseSeconds.CollectAllSumValues().Single(), Is.GreaterThan(0).After(200, 10));
        }

        [Test]
        public void When_a_garbage_collection_is_performed_then_the_gc_cpu_and_pause_ratios_can_be_calculated()
        {
            // arrange
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            Assert.That(() => StatsCollector.GcPauseSeconds.CollectAllCountValues().First(), Is.GreaterThan(0).After(5000, 100));
            
            // act
            StatsCollector.UpdateMetrics();

            // assert
            Assert.That(StatsCollector.GcPauseRatio.Value, Is.GreaterThan(0));
            Assert.That(StatsCollector.GcCpuRatio.Value, Is.GreaterThan(0));
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
}