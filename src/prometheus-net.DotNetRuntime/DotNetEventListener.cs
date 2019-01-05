using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Tracing;
using System.Linq;
using Prometheus.DotNetRuntime.StatsCollectors;

namespace Prometheus.DotNetRuntime
{
    internal sealed class DotNetEventListener : EventListener
    {
        private readonly IEventSourceStatsCollector _collector;

        internal DotNetEventListener(IEventSourceStatsCollector collector) : base()
        {
            _collector = collector;
            EnableEventSources(collector);
        }

        private void EnableEventSources(IEventSourceStatsCollector forCollector)
        {
            EventSourceCreated += (sender, e) =>
            {
                var es = e.EventSource;
                if (es.Guid == forCollector.EventSourceGuid)
                {
                    EnableEvents(es, forCollector.Level, forCollector.Keywords);
                }
            };
        }
        
        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            _collector.ProcessEvent(eventData);
        }
    }
}