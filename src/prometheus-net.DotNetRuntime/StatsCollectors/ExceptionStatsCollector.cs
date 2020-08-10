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
        private const string LabelReason = "exception";

        internal Counter ExceptionReasons { get; private set; }

        public Guid EventSourceGuid => DotNetRuntimeEventSource.Id;

        public EventKeywords Keywords => (EventKeywords)DotNetRuntimeEventSource.Keywords.Exception;
        public EventLevel Level => EventLevel.Informational;

        public void RegisterMetrics(MetricFactory metrics)
        {
            ExceptionReasons = metrics.CreateCounter(
                "dotnet_exception_reasons_total",
                "Reasons that led to an exception",
                LabelReason
            );
        }

        public void UpdateMetrics()
        {

        }

        public void ProcessEvent(EventWrittenEventArgs e)
        {
            if (e.EventId == EventIdExceptionThrown)
            {
                ExceptionReasons.Labels((string)e.Payload[0]).Inc();
            }
        }
    }

}
