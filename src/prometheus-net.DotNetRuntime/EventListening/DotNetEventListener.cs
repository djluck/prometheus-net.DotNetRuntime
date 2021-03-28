using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using Prometheus.DotNetRuntime.Metrics.Producers.Util;

namespace Prometheus.DotNetRuntime.EventListening
{
    internal sealed class DotNetEventListener : EventListener
    {
        private readonly GlobalOptions _globalOptions;
        private readonly string _nameSnakeCase;
        private readonly HashSet<EventSource> _enabledEventSources = new();
        private readonly Stopwatch _sp;
        private HashSet<long> _threadIdsPublishingEvents;

        internal DotNetEventListener(IEventListener eventListener, EventLevel level, GlobalOptions globalOptions)
        {
            Level = level;
            EventListener = eventListener;
            _globalOptions = globalOptions;

            if (_globalOptions.EnabledDebuggingMetrics)
            {
                _nameSnakeCase = eventListener.GetType().Name.ToSnakeCase();
                _sp = new Stopwatch();
                _threadIdsPublishingEvents = new HashSet<long>();
            }
            
            EventSourceCreated += OnEventSourceCreated;
        }

        public EventLevel Level { get; }
        internal bool StartedReceivingEvents { get; private set; }
        internal IEventListener EventListener { get; private set; }
        
        private void OnEventSourceCreated(object sender, EventSourceCreatedEventArgs e)
        {
            var es = e.EventSource;
            if (es.Guid == EventListener.EventSourceGuid)
            {
                EnableEvents(es, Level, EventListener.Keywords,  GetEventListenerArguments(EventListener));
                _enabledEventSources.Add(e.EventSource);
                StartedReceivingEvents = true;
            }
        }

        private Dictionary<string, string> GetEventListenerArguments(IEventListener listener)
        {
            var args = new Dictionary<string, string>();
            if (listener is IEventCounterListener counterListener)
            {
                args["EventCounterIntervalSec"] = counterListener.RefreshIntervalSeconds.ToString();
            }

            return args;
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            try
            {
                if (_globalOptions.EnabledDebuggingMetrics)
                {
                    _globalOptions.DebuggingMetrics.EventTypeCounts.Labels(_nameSnakeCase, eventData.EventSource.Name, eventData.EventName ?? "unknown").Inc();
                    _sp.Restart();
                    _threadIdsPublishingEvents.Add(eventData.OSThreadId);
                    _globalOptions.DebuggingMetrics.ThreadCount.Labels(_nameSnakeCase).Set(_threadIdsPublishingEvents.Count);
                }
                
                // Event counters are present in every EventListener, regardless of if they subscribed to them.
                // Kind of odd but just filter them out by source here.
                if (eventData.EventSource.Guid == EventListener.EventSourceGuid)
                    EventListener.ProcessEvent(eventData);

                if (_globalOptions.EnabledDebuggingMetrics)
                {
                    _sp.Stop();
                    _globalOptions.DebuggingMetrics.TimeConsumed.Labels(_nameSnakeCase, eventData.EventSource.Name, eventData.EventName ?? "unknown").Inc(_sp.Elapsed.TotalSeconds);
                }
            }
            catch (Exception e)
            {
                _globalOptions.ErrorHandler(e);
            }
        }

        public override void Dispose()
        {
            EventSourceCreated -= OnEventSourceCreated;
            EventListener.Dispose();
            base.Dispose();
        }

        internal class GlobalOptions
        {
            internal GlobalOptions()
            {
            }

            internal static GlobalOptions CreateFrom(DotNetRuntimeStatsCollector.Options opts, MetricFactory factory)
            {
                var instance = new GlobalOptions();
                if (opts.EnabledDebuggingMetrics)
                {
                    instance.DebuggingMetrics = new(
                        factory.CreateCounter($"dotnet_debug_event_count_total", "The total number of .NET diagnostic events processed", "listener_name", "event_source_name", "event_name"),
                        factory.CreateCounter("dotnet_debug_event_seconds_total",
                            "The total time consumed by processing .NET diagnostic events (does not include the CPU cost to generate the events)",
                            "listener_name", "event_source_name", "event_name"),
                        factory.CreateGauge("dotnet_debug_publish_thread_count", "The number of threads that have published events", "listener_name")
                    );
                }

                instance.EnabledDebuggingMetrics = opts.EnabledDebuggingMetrics;
                instance.ErrorHandler = opts.ErrorHandler;

                return instance;
            }
            
            public Action<Exception> ErrorHandler { get; set; } = (e => { });
            public bool EnabledDebuggingMetrics { get; set; } = false;
            public DebugMetrics DebuggingMetrics { get; set; }

            public class DebugMetrics
            {
                public DebugMetrics(Counter eventTypeCounts, Counter timeConsumed, Gauge threadCount)
                {
                    EventTypeCounts = eventTypeCounts;
                    TimeConsumed = timeConsumed;
                    ThreadCount = threadCount;
                }
                public Counter TimeConsumed { get; }
                public Counter EventTypeCounts { get; }
                public Gauge ThreadCount { get; }
            }
        }
    }
}