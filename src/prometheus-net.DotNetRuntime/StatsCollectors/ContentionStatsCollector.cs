using System;
using System.Diagnostics.Tracing;
#if PROMV2
using Prometheus.Advanced;
#endif
using Prometheus.DotNetRuntime.EventSources;
using Prometheus.DotNetRuntime.StatsCollectors.Util;

namespace Prometheus.DotNetRuntime.StatsCollectors
{
    /// <summary>
    /// Measures the level of contention in a .NET process, capturing the number 
    /// of locks contended and the total amount of time spent contending a lock.
    /// </summary>
    /// <remarks>
    /// Due to the way ETW events are triggered, only monitors contended will fire an event- spin locks, etc.
    /// do not trigger contention events and so cannot be tracked.
    /// </remarks>
    internal sealed class ContentionStatsCollector : IEventSourceStatsCollector
    {
        private const int EventIdContentionStart = 81, EventIdContentionStop = 91;
        private readonly EventPairTimer<long> _eventPairTimer = new EventPairTimer<long>(EventIdContentionStart, EventIdContentionStop, x => x.OSThreadId);
        
        public EventKeywords Keywords => (EventKeywords) DotNetRuntimeEventSource.Keywords.Contention;
        public EventLevel Level => EventLevel.Informational;
        public Guid EventSourceGuid => DotNetRuntimeEventSource.Id;
        
        internal Counter ContentionSecondsTotal { get; private set; }
        internal Counter ContentionTotal { get; private set; }

        public void RegisterMetrics(MetricFactory metrics)
        {
            ContentionSecondsTotal = metrics.CreateCounter("dotnet_contention_seconds_total", "The total amount of time spent contending locks");
            ContentionTotal = metrics.CreateCounter("dotnet_contention_total", "The number of locks contended");
        }

        public void UpdateMetrics()
        {
        }

        public void ProcessEvent(EventWrittenEventArgs e)
        {
            if (_eventPairTimer.TryGetEventPairDuration(e, out var duration))
            {
                ContentionTotal.Inc();
                ContentionSecondsTotal.Inc(duration.TotalSeconds);    
            }
        }
    }
}