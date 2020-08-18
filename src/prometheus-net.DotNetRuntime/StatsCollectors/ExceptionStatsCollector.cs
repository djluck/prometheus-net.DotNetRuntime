using Prometheus.DotNetRuntime.EventSources;
using System;
using System.Diagnostics.Tracing;
#if PROMV2
using Prometheus.Advanced;
#endif


namespace Prometheus.DotNetRuntime.StatsCollectors
{
    public class ExceptionStatsCollector : IEventSourceStatsCollector
    {
        private const int EventIdExceptionThrown = 80;
        private const string LabelType = "type";

        internal Counter ExceptionCount { get; private set; }

        public Guid EventSourceGuid => DotNetRuntimeEventSource.Id;

        public EventKeywords Keywords => (EventKeywords)DotNetRuntimeEventSource.Keywords.Exception;
        public EventLevel Level => EventLevel.Informational;

        public void RegisterMetrics(MetricFactory metrics)
        {
            ExceptionCount = metrics.CreateCounter(
                "dotnet_exceptions_total",
                "Count of exceptions broken down by type",
                LabelType
            );
        }

        public void UpdateMetrics()
        {

        }

        public void ProcessEvent(EventWrittenEventArgs e)
        {
            if (e.EventId == EventIdExceptionThrown)
            {
                ExceptionCount.Labels((string)e.Payload[0]).Inc();
            }
        }
    }

}
