using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
#if PROMV2
using Prometheus.Advanced;
#endif
using Prometheus.DotNetRuntime.EventSources;
using Prometheus.DotNetRuntime.StatsCollectors.Util;

namespace Prometheus.DotNetRuntime.StatsCollectors
{
    /// <summary>
    /// Measures how the frequency and duration of garbage collections and volume of allocations. Includes information
    ///  such as the generation the collection is running for, what triggered the collection and the type of the collection.
    /// </summary>
    internal sealed class GcStatsCollector : IEventSourceStatsCollector
    {
        private const string
            LabelHeap = "gc_heap",
            LabelGeneration = "gc_generation",
            LabelReason = "gc_reason",
            LabelType = "gc_type";

        private const int
            EventIdGcStart = 1,
            EventIdGcStop = 2,
            EventIdSuspendEEStart = 9,
            EventIdRestartEEStop = 3,
            EventIdHeapStats = 4,
            EventIdAllocTick = 10;

        private readonly EventPairTimer<uint, GcData> _gcEventTimer = new EventPairTimer<uint, GcData>(
            EventIdGcStart,
            EventIdGcStop,
            x => (uint) x.Payload[0],
            x => new GcData((uint) x.Payload[1], (DotNetRuntimeEventSource.GCType) x.Payload[3]));

        private readonly EventPairTimer<int> _gcPauseEventTimer = new EventPairTimer<int>(
            EventIdSuspendEEStart,
            EventIdRestartEEStop,
            // Suspensions/ Resumptions are always done sequentially so there is no common value to match events on. Return a constant value as the event id.
            x => 1);

        private readonly Dictionary<DotNetRuntimeEventSource.GCReason, string> _gcReasonToLabels = LabelGenerator.MapEnumToLabelValues<DotNetRuntimeEventSource.GCReason>();
        private readonly Ratio _gcCpuRatio = Ratio.ProcessTotalCpu();
        private readonly Ratio _gcPauseRatio = Ratio.ProcessTime();
        private readonly double[] _histogramBuckets;

        public GcStatsCollector(double[] histogramBuckets)
        {
            _histogramBuckets = histogramBuckets;
        }

        public GcStatsCollector() : this(Constants.DefaultHistogramBuckets)
        {
        }

        public Guid EventSourceGuid => DotNetRuntimeEventSource.Id;
        public EventKeywords Keywords => (EventKeywords) DotNetRuntimeEventSource.Keywords.GC;
        public EventLevel Level => EventLevel.Verbose;

        internal Histogram GcCollectionSeconds { get; private set; }
        internal Histogram GcPauseSeconds { get; private set; }
        internal Counter GcCollectionReasons { get; private set; }
        internal Gauge GcCpuRatio { get; private set; }
        internal Gauge GcPauseRatio { get; private set; }
        internal Counter AllocatedBytes { get; private set; }
        internal Gauge GcHeapSizeBytes { get; private set; }
        internal Gauge GcNumPinnedObjects { get; private set; }
        internal Gauge GcFinalizationQueueLength { get; private set; }

        public void RegisterMetrics(MetricFactory metrics)
        {
            GcCollectionSeconds = metrics.CreateHistogram(
                "dotnet_gc_collection_seconds",
                "The amount of time spent running garbage collections",
                    new HistogramConfiguration()
                    {
                        Buckets = _histogramBuckets,
                        LabelNames = new []{ LabelGeneration, LabelType }
                    }
                );

            GcPauseSeconds = metrics.CreateHistogram(
                "dotnet_gc_pause_seconds",
                "The amount of time execution was paused for garbage collection",
                new HistogramConfiguration()
                    {
                        Buckets = _histogramBuckets
                    }
                );

            GcCollectionReasons = metrics.CreateCounter(
                "dotnet_gc_collection_reasons_total",
                "A tally of all the reasons that lead to garbage collections being run",
                LabelReason);

            GcCpuRatio = metrics.CreateGauge("dotnet_gc_cpu_ratio", "The percentage of process CPU time spent running garbage collections");
            GcPauseRatio = metrics.CreateGauge("dotnet_gc_pause_ratio", "The percentage of time the process spent paused for garbage collection");

            AllocatedBytes = metrics.CreateCounter(
                "dotnet_gc_allocated_bytes_total",
                "The total number of bytes allocated on the small and large object heaps (updated every 100KB of allocations)",
                LabelHeap);

            GcHeapSizeBytes = metrics.CreateGauge(
                "dotnet_gc_heap_size_bytes",
                "The current size of all heaps (only updated after a garbage collection)",
                LabelGeneration);

            GcNumPinnedObjects = metrics.CreateGauge("dotnet_gc_pinned_objects", "The number of pinned objects");
            GcFinalizationQueueLength = metrics.CreateGauge("dotnet_gc_finalization_queue_length", "The number of objects waiting to be finalized");
        }

        public void UpdateMetrics()
        {
            GcCpuRatio.Set(_gcCpuRatio.CalculateConsumedRatio(GcCollectionSeconds));
            GcPauseRatio.Set(_gcPauseRatio.CalculateConsumedRatio(GcPauseSeconds));
        }

        public void ProcessEvent(EventWrittenEventArgs e)
        {
            if (e.EventId == EventIdAllocTick)
            {
                const uint lohHeapFlag = 0x1;
                var heapLabelValue = ((uint) e.Payload[1] & lohHeapFlag) == lohHeapFlag ? "loh" : "soh";
                AllocatedBytes.Labels(heapLabelValue).Inc((uint) e.Payload[0]);
                return;
            }

            if (e.EventId == EventIdHeapStats)
            {
                GcHeapSizeBytes.Labels("0").Set((ulong) e.Payload[0]);
                GcHeapSizeBytes.Labels("1").Set((ulong) e.Payload[2]);
                GcHeapSizeBytes.Labels("2").Set((ulong) e.Payload[4]);
                GcHeapSizeBytes.Labels("loh").Set((ulong) e.Payload[6]);
                GcFinalizationQueueLength.Set((ulong) e.Payload[9]);
                GcNumPinnedObjects.Set((uint) e.Payload[10]);
                return;
            }

            // flags representing the "Garbage Collection" + "Preparation for garbage collection" pause reasons
            const uint suspendGcReasons = 0x1 | 0x6;

            if (e.EventId == EventIdSuspendEEStart && ((uint) e.Payload[0] & suspendGcReasons) == 0)
            {
                // Execution engine is pausing for a reason other than GC, discard event.
                return;
            }

            if (_gcPauseEventTimer.TryGetEventPairDuration(e, out var pauseDuration))
            {
                GcPauseSeconds.Observe(pauseDuration.TotalSeconds);
                return;
            }

            if (e.EventId == EventIdGcStart)
            {
                GcCollectionReasons.Labels(_gcReasonToLabels[(DotNetRuntimeEventSource.GCReason) e.Payload[2]]).Inc();
            }

            if (_gcEventTimer.TryGetEventPairDuration(e, out var gcDuration, out var gcData))
            {
                GcCollectionSeconds.Labels(gcData.GetGenerationToString(), gcData.GetTypeToString()).Observe(gcDuration.TotalSeconds);
            }
        }

        private struct GcData
        {
            private static readonly Dictionary<DotNetRuntimeEventSource.GCType, string> GcTypeToLabels = LabelGenerator.MapEnumToLabelValues<DotNetRuntimeEventSource.GCType>();

            public GcData(uint generation, DotNetRuntimeEventSource.GCType type)
            {
                Generation = generation;
                Type = type;
            }

            public uint Generation { get; }
            public DotNetRuntimeEventSource.GCType Type { get; }

            public string GetTypeToString()
            {
                return GcTypeToLabels[Type];
            }

            public string GetGenerationToString()
            {
                if (Generation > 2)
                {
                    return "loh";
                }

                return Generation.ToString();
            }
        }
    }
}