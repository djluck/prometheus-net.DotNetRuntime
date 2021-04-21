using System;
using System.Diagnostics.Tracing;
using Prometheus.DotNetRuntime.EventListening.EventSources;
using Prometheus.DotNetRuntime.EventListening.Parsers.Util;

namespace Prometheus.DotNetRuntime.EventListening.Parsers
{
    public class ContentionEventParser : IEventParser<ContentionEventParser>, ContentionEventParser.Events.Info
    {
        private readonly SamplingRate _samplingRate;
        private const int EventIdContentionStart = 81, EventIdContentionStop = 91;
        private readonly EventPairTimer<long> _eventPairTimer;
        
        public event Action<Events.ContentionStartEvent> ContentionStart;
        public event Action<Events.ContentionEndEvent> ContentionEnd;

        public ContentionEventParser(SamplingRate samplingRate)
        {
            _samplingRate = samplingRate;
            _eventPairTimer = new EventPairTimer<long>(
                EventIdContentionStart,
                EventIdContentionStop,
                x => x.OSThreadId,
                samplingRate
            );
        }

        public EventKeywords Keywords => (EventKeywords)DotNetRuntimeEventSource.Keywords.Contention;
        public Guid EventSourceGuid => DotNetRuntimeEventSource.Id;
        
        public void ProcessEvent(EventWrittenEventArgs e)
        {
            switch (_eventPairTimer.TryGetDuration(e, out var duration))
            {
                case DurationResult.Start:
                    ContentionStart?.Invoke(Events.ContentionStartEvent.Instance);
                    return;
                
                case DurationResult.FinalWithDuration:
                    ContentionEnd?.InvokeManyTimes(_samplingRate.SampleEvery, Events.ContentionEndEvent.GetFrom(duration));
                    return;

                default:
                    return;
            }
        }

        public static class Events
        {
            public interface Info : IInfoEvents
            {
                event Action<ContentionStartEvent> ContentionStart;
                event Action<ContentionEndEvent> ContentionEnd;
            }
            
            public class ContentionStartEvent
            {
                public static readonly ContentionStartEvent Instance = new();
            
                private ContentionStartEvent()
                {
                }
            }
        
            public class ContentionEndEvent
            {
                private static readonly ContentionEndEvent Instance = new();
            
                private ContentionEndEvent()
                {
                }
            
                public TimeSpan ContentionDuration { get; private set; }
            
                public static ContentionEndEvent GetFrom(TimeSpan contentionDuration)
                {
                    Instance.ContentionDuration = contentionDuration;
                    return Instance;
                }
            }
        }
    }
}