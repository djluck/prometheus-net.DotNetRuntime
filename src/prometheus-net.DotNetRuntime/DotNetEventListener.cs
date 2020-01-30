using Prometheus.DotNetRuntime.StatsCollectors.Util;
using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;

namespace Prometheus.DotNetRuntime
{
    internal sealed class DotNetEventListener : EventListener
    {
        private static Counter _eventTypeCounts;
        private static Counter _cpuConsumed;

        private readonly IEventSourceStatsCollector _collector;
        private readonly Action<Exception> _errorHandler;
        private readonly bool _enableDebugging;
        private readonly string _nameSnakeCase;

        internal DotNetEventListener(IEventSourceStatsCollector collector, Action<Exception> errorHandler, bool enableDebugging) : base()
        {
            _collector = collector;
            _errorHandler = errorHandler;
            _enableDebugging = enableDebugging;

            if (_enableDebugging)
            {
                _eventTypeCounts ??= Metrics.CreateCounter($"dotnet_debug_events_total", "The total number of .NET diagnostic events processed", "collector_name", "event_source_name", "event_name");
                _cpuConsumed ??= Metrics.CreateCounter("dotnet_debug_cpu_seconds_total", "The total CPU time consumed by processing .NET diagnostic events (does not include the CPU cost to generate the events)", "collector_name", "event_source_name", "event_name");
                _nameSnakeCase = collector.GetType().Name.ToSnakeCase();
            }
            EventSourceCreated += OnEventSourceCreated;
        }

        internal bool StartedReceivingEvents { get; private set; }

        private void OnEventSourceCreated(object sender, EventSourceCreatedEventArgs e)
        {
            var es = e.EventSource;
            if (es.Guid == _collector.EventSourceGuid)
            {
                EnableEvents(es, _collector.Level, _collector.Keywords);
                StartedReceivingEvents = true;
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            var sp = new Stopwatch();
            try
            {
                if (_enableDebugging)
                {
                    _eventTypeCounts.Labels(_nameSnakeCase, eventData.EventSource.Name, eventData.EventName).Inc();
                    sp.Restart();
                }

                _collector.ProcessEvent(eventData);

                if (_enableDebugging)
                {
                    sp.Stop();
                    _cpuConsumed.Labels(_nameSnakeCase, eventData.EventSource.Name, eventData.EventName).Inc(sp.Elapsed.TotalSeconds);
                }
            }
            catch (Exception e)
            {
                _errorHandler(e);
            }
        }

        public override void Dispose()
        {
            EventSourceCreated -= OnEventSourceCreated;
            base.Dispose();
        }
    }
}