using System;
using System.Collections.Generic;
using Prometheus.DotNetRuntime.EventListening.Parsers;
using Prometheus.DotNetRuntime.EventListening;
using Prometheus.DotNetRuntime.EventListening.EventSources;
using Prometheus.DotNetRuntime.Metrics.Producers.Util;


namespace Prometheus.DotNetRuntime.Metrics.Producers
{
    public class GcMetricsProducer : IMetricProducer
    {
        private const string
            LabelHeap = "gc_heap",
            LabelGeneration = "gc_generation",
            LabelReason = "gc_reason",
            LabelType = "gc_type";
        
        private static readonly Dictionary<DotNetRuntimeEventSource.GCType, string> GcTypeToLabels = LabelGenerator.MapEnumToLabelValues<DotNetRuntimeEventSource.GCType>();
        private static readonly Dictionary<DotNetRuntimeEventSource.GCReason, string> GcReasonToLabels = LabelGenerator.MapEnumToLabelValues<DotNetRuntimeEventSource.GCReason>();
        
        private readonly Consumes<GcEventParser.Events.Info> _gcInfo;
        private readonly Consumes<GcEventParser.Events.Verbose> _gcVerbose;
        private readonly Consumes<RuntimeEventParser.Events.CountersV3_0> _runtimeCounters;
        private readonly Ratio _gcCpuRatio = Ratio.ProcessTotalCpu();
        private readonly Ratio _gcPauseRatio = Ratio.ProcessTime();
        private readonly Options _options;
        
        public GcMetricsProducer(
            Options options,
            Consumes<GcEventParser.Events.Info> gcInfo,
            Consumes<GcEventParser.Events.Verbose> gcVerbose,
            Consumes<RuntimeEventParser.Events.CountersV3_0> runtimeCounters)
        {
            _options = options;
            _gcInfo = gcInfo;
            _gcVerbose = gcVerbose;
            _runtimeCounters = runtimeCounters;
        }
        
        internal Histogram GcCollectionSeconds { get; private set; }
        internal Histogram GcPauseSeconds { get; private set; }
        internal Counter GcCollections { get; private set; }
        internal Gauge GcCpuRatio { get; private set; }
        internal Gauge GcPauseRatio { get; private set; }
        internal Counter AllocatedBytes { get; private set; }
        internal Gauge GcHeapSizeBytes { get; private set; }
        internal Gauge GcNumPinnedObjects { get; private set; }
        internal Gauge GcFinalizationQueueLength { get; private set; }
        internal Gauge AvailableMemory { get; private set; }

        public void RegisterMetrics(MetricFactory metrics)
        {
#if !NETSTANDARD2_1
            AvailableMemory = metrics.CreateGauge("dotnet_gc_memory_total_available_bytes", "The upper limit on the amount of physical memory .NET can allocate to");
#endif
            
            // No registered sources available- cannot produce metrics 
            if (!_gcInfo.Enabled && !_gcVerbose.Enabled && !_runtimeCounters.Enabled)
                return;

            if (_runtimeCounters.Enabled && !_gcInfo.Enabled)
            {
                GcPauseRatio = metrics.CreateGauge("dotnet_gc_pause_ratio", "The percentage of time the process spent paused for garbage collection");
                _runtimeCounters.Events.TimeInGc += e => GcPauseRatio.Set(e.Mean /  100.0);
                
                GcHeapSizeBytes = metrics.CreateGauge(
                    "dotnet_gc_heap_size_bytes",
                    "The current size of all heaps (only updated after a garbage collection)",
                    LabelGeneration);

                _runtimeCounters.Events.Gen0Size += e => GcHeapSizeBytes.Labels("0").Set(e.Mean);
                _runtimeCounters.Events.Gen1Size += e => GcHeapSizeBytes.Labels("1").Set(e.Mean);
                _runtimeCounters.Events.Gen2Size += e => GcHeapSizeBytes.Labels("2").Set(e.Mean);
                _runtimeCounters.Events.LohSize += e => GcHeapSizeBytes.Labels("loh").Set(e.Mean);
                
                GcCollections = metrics.CreateCounter(
                    "dotnet_gc_collection_count_total",
                    "Counts the number of garbage collections that have occurred, broken down by generation number.",
                    LabelGeneration);

                _runtimeCounters.Events.Gen0GcCount += e => GcCollections.Labels("0").Inc(e.IncrementedBy);
                _runtimeCounters.Events.Gen1GcCount += e => GcCollections.Labels("1").Inc(e.IncrementedBy);
                _runtimeCounters.Events.Gen2GcCount += e => GcCollections.Labels("2").Inc(e.IncrementedBy);
            }

            if (_gcInfo.Enabled)
            {
                GcCollectionSeconds = metrics.CreateHistogram(
                    "dotnet_gc_collection_seconds",
                    "The amount of time spent running garbage collections",
                    new HistogramConfiguration()
                    {
                        Buckets = _options.HistogramBuckets,
                        LabelNames = new[] {LabelGeneration, LabelType}
                    }
                );

                _gcInfo.Events.CollectionComplete += (e) => GcCollectionSeconds.Labels(GetGenerationToString(e.Generation), GcTypeToLabels[e.Type]).Observe(e.Duration.TotalSeconds);

                GcPauseSeconds = metrics.CreateHistogram(
                    "dotnet_gc_pause_seconds",
                    "The amount of time execution was paused for garbage collection",
                    new HistogramConfiguration()
                    {
                        Buckets = _options.HistogramBuckets
                    }
                );

                _gcInfo.Events.PauseComplete += (e) => GcPauseSeconds.Observe(e.PauseDuration.TotalSeconds);

                GcCollections = metrics.CreateCounter(
                    "dotnet_gc_collection_count_total",
                    "Counts the number of garbage collections that have occurred, broken down by generation number and the reason for the collection.",
                    LabelGeneration, LabelReason);

                _gcInfo.Events.CollectionStart += (e) => GcCollections.Labels(GetGenerationToString(e.Generation), GcReasonToLabels[e.Reason]).Inc();

                GcCpuRatio = metrics.CreateGauge("dotnet_gc_cpu_ratio", "The percentage of process CPU time spent running garbage collections");
                GcPauseRatio = metrics.CreateGauge("dotnet_gc_pause_ratio", "The percentage of time the process spent paused for garbage collection");
                
                GcHeapSizeBytes = metrics.CreateGauge(
                    "dotnet_gc_heap_size_bytes",
                    "The current size of all heaps (only updated after a garbage collection)",
                    LabelGeneration);

                GcNumPinnedObjects = metrics.CreateGauge("dotnet_gc_pinned_objects", "The number of pinned objects");
                GcFinalizationQueueLength = metrics.CreateGauge("dotnet_gc_finalization_queue_length", "The number of objects waiting to be finalized");

                _gcInfo.Events.HeapStats += e =>
                {
                    GcHeapSizeBytes.Labels("0").Set(e.Gen0SizeBytes);
                    GcHeapSizeBytes.Labels("1").Set(e.Gen1SizeBytes);
                    GcHeapSizeBytes.Labels("2").Set(e.Gen2SizeBytes);
                    GcHeapSizeBytes.Labels("loh").Set(e.LohSizeBytes);
                    GcFinalizationQueueLength.Set(e.FinalizationQueueLength);
                    GcNumPinnedObjects.Set(e.NumPinnedObjects);
                };
            }

            if (_gcVerbose.Enabled || _runtimeCounters.Enabled)
            {
                AllocatedBytes = metrics.CreateCounter(
                    "dotnet_gc_allocated_bytes_total",
                    "The total number of bytes allocated on the managed heap",
                    labelNames: _gcVerbose.Enabled ? new [] { LabelHeap } : new string[0]);
                    
                if (_gcVerbose.Enabled)
                    _gcVerbose.Events.AllocationTick += e => AllocatedBytes.Labels(e.IsLargeObjectHeap ? "loh" : "soh").Inc(e.AllocatedBytes);
                else
                    _runtimeCounters.Events.AllocRate += r => AllocatedBytes.Inc(r.IncrementedBy);
            }
        }

        public void UpdateMetrics()
        {
            if (_gcInfo.Enabled)
            {
                GcCpuRatio?.Set(_gcCpuRatio.CalculateConsumedRatio(GcCollectionSeconds));
                GcPauseRatio?.Set(_gcPauseRatio.CalculateConsumedRatio(GcPauseSeconds));
            }

#if !NETSTANDARD2_1
            AvailableMemory?.Set(GC.GetGCMemoryInfo().TotalAvailableMemoryBytes);
#endif
        }

        private static string GetGenerationToString(uint generation)
        {
            return generation switch
            {
                0 => "0",
                1 => "1",
                2 => "2",
                // large object heap
                3 => "loh",
                // pinned object heap, .NET 5+ only
                4 => "poh",
                _ => generation.ToString()
            };
        }
        
        public class Options
        {
            public double[] HistogramBuckets { get; set; } = Constants.DefaultHistogramBuckets;
        }
    }
}