using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Prometheus.DotNetRuntime.EventListening.EventSources;
using Prometheus.DotNetRuntime.EventListening.Parsers;
using Prometheus.DotNetRuntime.Metrics.Producers.Util;

namespace Prometheus.DotNetRuntime.Metrics.Producers
{
    public class ThreadPoolMetricsProducer : IMetricProducer
    {
        private readonly Dictionary<DotNetRuntimeEventSource.ThreadAdjustmentReason, string> _adjustmentReasonToLabel = LabelGenerator.MapEnumToLabelValues<DotNetRuntimeEventSource.ThreadAdjustmentReason>();
        private readonly Options _options;
        private readonly Consumes<ThreadPoolEventParser.Events.Info> _threadPoolInfo;
        private readonly Consumes<RuntimeEventParser.Events.CountersV3_0> _runtimeCounters;

        public ThreadPoolMetricsProducer(Options options, Consumes<ThreadPoolEventParser.Events.Info> threadPoolInfo, Consumes<RuntimeEventParser.Events.CountersV3_0> runtimeCounters)
        {
            _options = options;
            _threadPoolInfo = threadPoolInfo;
            _runtimeCounters = runtimeCounters;
        }
        
        internal Gauge NumThreads { get; private set; }
        internal Gauge NumIocThreads { get; private set; }
        internal Counter AdjustmentsTotal { get; private set; }
        internal Counter Throughput { get; private set; }
        internal Histogram QueueLength { get; private set; }
        internal Gauge NumTimers { get; private set; }

        public void RegisterMetrics(MetricFactory metrics)
        {
            if (!_threadPoolInfo.Enabled && !_runtimeCounters.Enabled)
                return;
            
            NumThreads = metrics.CreateGauge("dotnet_threadpool_num_threads", "The number of active threads in the thread pool");
            _runtimeCounters.Events.ConnectionCount += e => NumThreads.Set(e.Mean);
                
            Throughput = metrics.CreateCounter("dotnet_threadpool_throughput_total", "The total number of work items that have finished execution in the thread pool");
            _runtimeCounters.Events.ConnectionCompletedItemsCount += e => Throughput.Inc(e.IncrementedBy);

            QueueLength = metrics.CreateHistogram("dotnet_threadpool_queue_length",
                "Measures the queue length of the thread pool. Values greater than 0 indicate a backlog of work for the threadpool to process.",
                new HistogramConfiguration {Buckets = _options.QueueLengthHistogramBuckets}
            );
            _runtimeCounters.Events.ThreadPoolQueueLength += e => QueueLength.Observe(e.Mean);

            NumTimers = metrics.CreateGauge("dotnet_threadpool_timer_count", "The number of timers active");
            _runtimeCounters.Events.ActiveTimerCount += e => NumTimers.Set(e.Mean);
            
            if (_threadPoolInfo.Enabled)
            {
                AdjustmentsTotal = metrics.CreateCounter(
                    "dotnet_threadpool_adjustments_total",
                    "The total number of changes made to the size of the thread pool, labeled by the reason for change",
                    "adjustment_reason");
                
                _threadPoolInfo.Events.ThreadPoolAdjusted += e =>
                {
                    AdjustmentsTotal.Labels(_adjustmentReasonToLabel[e.AdjustmentReason]).Inc();
                };
                
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // IO threadpool only exists on windows
                    NumIocThreads = metrics.CreateGauge("dotnet_threadpool_io_num_threads", "The number of active threads in the IO thread pool");
                    _threadPoolInfo.Events.IoThreadPoolAdjusted += e => NumIocThreads.Set(e.NumThreads);
                }
            }
        }


        public void UpdateMetrics()
        {
        }

        public class Options
        {
            public double[] QueueLengthHistogramBuckets { get; set; } = new double[] { 0, 1, 10, 100, 1000 };
        }
    }
}