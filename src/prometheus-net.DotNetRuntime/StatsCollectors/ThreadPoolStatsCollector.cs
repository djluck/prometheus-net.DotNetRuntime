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
    /// Measures the size of the worker + IO thread pools, worker pool throughput and reasons for worker pool
    /// adjustments. 
    /// </summary>
    public class ThreadPoolStatsCollector : IEventSourceStatsCollector
    {
        private const int
            EventIdThreadPoolSample = 54,
            EventIdThreadPoolAdjustment = 55,
            EventIdIoThreadCreate = 44,
            EventIdIoThreadRetire = 46,
            EventIdIoThreadUnretire = 47,
            EventIdIoThreadTerminate = 45;

        private Dictionary<DotNetRuntimeEventSource.ThreadAdjustmentReason, string> _adjustmentReasonToLabel = LabelGenerator.MapEnumToLabelValues<DotNetRuntimeEventSource.ThreadAdjustmentReason>();

        internal Gauge NumThreads { get; private set; }
        internal Gauge NumIocThreads { get; private set; }
        
        // TODO resolve issue where throughput cannot be calculated (stats event is giving garbage values)
        // internal Counter Throughput { get; private set; }
        internal Counter AdjustmentsTotal { get; private set; }

        public Guid EventSourceGuid => DotNetRuntimeEventSource.Id;
        public EventKeywords Keywords => (EventKeywords) DotNetRuntimeEventSource.Keywords.Threading;
        public EventLevel Level => EventLevel.Informational;

        public void RegisterMetrics(MetricFactory metrics)
        {
            NumThreads = metrics.CreateGauge("dotnet_threadpool_num_threads", "The number of active threads in the thread pool");
            NumIocThreads = metrics.CreateGauge("dotnet_threadpool_io_num_threads", "The number of active threads in the IO thread pool");
            // Throughput = metrics.CreateCounter("dotnet_threadpool_throughput_total", "The total number of work items that have finished execution in the thread pool");
            AdjustmentsTotal = metrics.CreateCounter(
                "dotnet_threadpool_adjustments_total",
                "The total number of changes made to the size of the thread pool, labeled by the reason for change",
                "adjustment_reason");
        }

        public void UpdateMetrics()
        {
        }

        public void ProcessEvent(EventWrittenEventArgs e)
        {
            switch (e.EventId)
            {
                case EventIdThreadPoolSample:
                    // Throughput.Inc((double) e.Payload[0]);
                    return;

                case EventIdThreadPoolAdjustment:
                    NumThreads.Set((uint) e.Payload[1]);
                    AdjustmentsTotal.Labels(_adjustmentReasonToLabel[(DotNetRuntimeEventSource.ThreadAdjustmentReason) e.Payload[2]]).Inc();
                    return;

                case EventIdIoThreadCreate:
                case EventIdIoThreadRetire:
                case EventIdIoThreadUnretire:
                case EventIdIoThreadTerminate:
                    NumIocThreads.Set((uint)e.Payload[0]);
                    return;
            }
        }
    }
}