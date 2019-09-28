using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using Prometheus.DotNetRuntime.StatsCollectors;
using Prometheus.DotNetRuntime.StatsCollectors.Util;

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
            
            EnableEventSources(collector);
        }
        
        internal bool StartedReceivingEvents { get; private set; }

        private void EnableEventSources(IEventSourceStatsCollector forCollector)
        {
            EventSourceCreated += (sender, e) =>
            {
                var es = e.EventSource;
                if (es.Guid == forCollector.EventSourceGuid)
                {
                    EnableEvents(es, forCollector.Level, forCollector.Keywords);
                    StartedReceivingEvents = true;
                }
            };
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
    }
}