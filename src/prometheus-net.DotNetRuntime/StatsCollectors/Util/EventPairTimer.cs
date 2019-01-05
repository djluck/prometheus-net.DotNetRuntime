using System;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;

namespace Prometheus.DotNetRuntime.StatsCollectors.Util
{
    /// <summary>
    /// To generate metrics, we are often interested in the duration between two events. This class
    /// helps time the duration between two events.
    /// </summary>
    /// <typeparam name="TId">A type of an identifier present on both events</typeparam>
    /// <typeparam name="TEventData">A struct that represents data of interest extracted from the first event</typeparam>
    public class EventPairTimer<TId, TEventData>
        where TId : struct
        where TEventData : struct
    {
        private readonly Cache<TId, EventDataWrapper> _eventStartedAtCache = new Cache<TId, EventDataWrapper>(TimeSpan.FromMinutes(5)); 
        private readonly int _startEventId;
        private readonly int _endEventId;
        private readonly Func<EventWrittenEventArgs, TId> _extractEventIdFn;
        private readonly Func<EventWrittenEventArgs, TEventData> _extractData;

        public EventPairTimer(int startEventId, int endEventId, Func<EventWrittenEventArgs, TId> extractEventIdFn, Func<EventWrittenEventArgs, TEventData> extractData)
        {
            _startEventId = startEventId;
            _endEventId = endEventId;
            _extractEventIdFn = extractEventIdFn;
            _extractData = extractData;
        }
        
        /// <summary>
        /// Checks if an event is an expected final event- if so, returns true, the duration between it and the start event and
        /// any data extracted from the first event.
        /// </summary>
        /// <remarks>
        /// If the event id matches the supplied start event id, then we cache the event until the final event occurs.
        /// All other events are ignored.
        /// </remarks>
        public bool TryGetEventPairDuration(EventWrittenEventArgs e, out TimeSpan duration, out TEventData startEventData)
        {
            duration = TimeSpan.Zero;
            startEventData = default(TEventData);
            
            if (e.EventId == _endEventId)
            {
                var id = _extractEventIdFn(e);
                if (_eventStartedAtCache.TryGetValue(id, out var startEvent))
                {
                    startEventData = startEvent.Data;
                    duration = e.TimeStamp - startEvent.TimeStamp;
                    return true;
                }
                else
                {
                    // TODO measure missing event
                    return false;
                }
            }
            
            if (e.EventId == _startEventId)
            {
                _eventStartedAtCache.Set(_extractEventIdFn(e), new EventDataWrapper(_extractData(e), e.TimeStamp));
            }

            return false;
        }

        private struct EventDataWrapper
        {
            public EventDataWrapper(TEventData data, DateTime timeStamp)
            {
                Data = data;
                TimeStamp = timeStamp;
            }
            
            public TEventData Data { get; }
            public DateTime TimeStamp { get; }
        }
    }
    
     public sealed class EventPairTimer<TId> : EventPairTimer<TId, int>
        where TId : struct
    {
      
        public EventPairTimer(int startEventId, int endEventId, Func<EventWrittenEventArgs, TId> extractEventIdFn) 
            : base(startEventId, endEventId, extractEventIdFn, e => 0)
        {
        }
        
        public bool TryGetEventPairDuration(EventWrittenEventArgs e, out TimeSpan duration)
        {
            return TryGetEventPairDuration(e, out duration, out _);
        }
    }
}