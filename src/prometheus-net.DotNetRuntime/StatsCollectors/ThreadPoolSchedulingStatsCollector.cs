using System;
using System.Diagnostics.Tracing;
using System.Linq;
#if PROMV2
using Prometheus.Advanced;
#endif
using Prometheus.DotNetRuntime.EventSources;
using Prometheus.DotNetRuntime.StatsCollectors.Util;

namespace Prometheus.DotNetRuntime.StatsCollectors
{
    /// <summary>
    /// Measures the volume of work scheduled on the thread pool and the delay between scheduling the work and it beginning execution.
    /// </summary>
    internal sealed class ThreadPoolSchedulingStatsCollector : IEventSourceStatsCollector
    {
        private const int EventIdThreadPoolEnqueueWork = 30, EventIdThreadPoolDequeueWork = 31;
        private readonly double[] _histogramBuckets;
        
        private readonly EventPairTimer<long> _eventPairTimer = new EventPairTimer<long>(
            EventIdThreadPoolEnqueueWork, 
            EventIdThreadPoolDequeueWork, 
            x => (long)x.Payload[0]
        );

        public ThreadPoolSchedulingStatsCollector(double[] histogramBuckets)
        {
            _histogramBuckets = histogramBuckets;
        }

        public ThreadPoolSchedulingStatsCollector(): this(Constants.DefaultHistogramBuckets)
        {
        }

        public EventKeywords Keywords => (EventKeywords) (FrameworkEventSource.Keywords.ThreadPool);
        public EventLevel Level => EventLevel.Verbose;
        public Guid EventSourceGuid => FrameworkEventSource.Id;
        
        internal Counter ScheduledCount { get; private set; }
        internal Histogram ScheduleDelay { get; private set; }

        public void RegisterMetrics(MetricFactory metrics)
        {
            ScheduledCount = metrics.CreateCounter("dotnet_threadpool_scheduled_total", "The total number of items the thread pool has been instructed to execute");
            ScheduleDelay = metrics.CreateHistogram(
                "dotnet_threadpool_scheduling_delay_seconds",
                "A breakdown of the latency experienced between an item being scheduled for execution on the thread pool and it starting execution.",
                new HistogramConfiguration()
                {
                    Buckets = _histogramBuckets
                }
            );
        }

        public void UpdateMetrics()
        {
        }
        
        public void ProcessEvent(EventWrittenEventArgs e)
        {
            if (e.EventId == EventIdThreadPoolEnqueueWork)
            {
                ScheduledCount.Inc();
            }
            
            if (_eventPairTimer.TryGetEventPairDuration(e, out var duration))
            {
                ScheduleDelay.Observe(duration.TotalSeconds);
            }   
        }
    }
}