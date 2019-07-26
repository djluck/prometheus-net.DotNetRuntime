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
        private readonly Cache<TId, TEventData> _eventStartedAtCache; 
        private readonly int _startEventId;
        private readonly int _endEventId;
        private readonly Func<EventWrittenEventArgs, TId> _extractEventIdFn;
        private readonly Func<EventWrittenEventArgs, TEventData> _extractData;
        private readonly SamplingRate _samplingRate;

        public EventPairTimer(
            int startEventId, 
            int endEventId, 
            Func<EventWrittenEventArgs, TId> extractEventIdFn, 
            Func<EventWrittenEventArgs, TEventData> extractData,
            SamplingRate samplingRate,
            Cache<TId, TEventData> cache = null)
        {
            _startEventId = startEventId;
            _endEventId = endEventId;
            _extractEventIdFn = extractEventIdFn;
            _extractData = extractData;
            _samplingRate = samplingRate;
            _eventStartedAtCache = cache ?? new Cache<TId, TEventData>(TimeSpan.FromMinutes(1));
        }
        
        /// <summary>
        /// Checks if an event is an expected final event- if so, returns true, the duration between it and the start event and
        /// any data extracted from the first event.
        /// </summary>
        /// <remarks>
        /// If the event id matches the supplied start event id, then we cache the event until the final event occurs.
        /// All other events are ignored.
        /// </remarks>
        public DurationResult TryGetDuration(EventWrittenEventArgs e, out TimeSpan duration, out TEventData startEventData)
        {
            duration = TimeSpan.Zero;
            startEventData = default(TEventData);
            
            if (e.EventId == _startEventId)
            {
                if (_samplingRate.ShouldSampleEvent())
                {
                    _eventStartedAtCache.Set(_extractEventIdFn(e), _extractData(e), e.TimeStamp);
                }

                return DurationResult.Start;
            }
            
            if (e.EventId == _endEventId)
            {
                var id = _extractEventIdFn(e);
                if (_eventStartedAtCache.TryRemove(id, out startEventData, out var timeStamp))
                {
                    duration = e.TimeStamp - timeStamp;
                    return DurationResult.FinalWithDuration;
                }
                else
                {
                    return DurationResult.FinalWithoutDuration;
                }
            }

            return DurationResult.Unrecognized;
        }
    }

    public enum DurationResult
    {
        Unrecognized = 0,
        Start = 1,
        FinalWithoutDuration = 2,
        FinalWithDuration = 3
    }
    
    public sealed class EventPairTimer<TId> : EventPairTimer<TId, int>
        where TId : struct
    {
        public EventPairTimer(int startEventId, int endEventId, Func<EventWrittenEventArgs, TId> extractEventIdFn, SamplingRate samplingRate, Cache<TId, int> cache = null) 
            : base(startEventId, endEventId, extractEventIdFn, e => 0, samplingRate, cache)
        {
        }
        
        public DurationResult TryGetDuration(EventWrittenEventArgs e, out TimeSpan duration)
        {
            return this.TryGetDuration(e, out duration, out _);
        }
    }
}