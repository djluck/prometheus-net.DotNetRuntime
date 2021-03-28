using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Tracing;
using System.Linq;
using Fasterflect;
using NUnit.Framework;

namespace Prometheus.DotNetRuntime.Tests.EventListening
{
    public class TestHelpers
    {
        public static EventWrittenEventArgs CreateEventWrittenEventArgs(int eventId, DateTime? timestamp = null, params object[] payload)
        {
            var args = (EventWrittenEventArgs)typeof(EventWrittenEventArgs).CreateInstance(new []{ typeof(EventSource)}, Flags.NonPublic | Flags.Instance, new object[] { null});
            args.SetPropertyValue("EventId", eventId);
            args.SetPropertyValue("Payload", new ReadOnlyCollection<object>(payload));
            
            if (timestamp.HasValue)
            {
                args.SetPropertyValue("TimeStamp", timestamp.Value);
            }
            
            return args;
        }
        
        public static EventWrittenEventArgs CreateCounterEventWrittenEventArgs(params (string key, object val)[] payload)
        {
            var counterPayload = payload.ToDictionary(k => k.key, v => v.val);

            var e = CreateEventWrittenEventArgs(-1, DateTime.UtcNow, new[] { counterPayload });
            e.SetPropertyValue("EventName", "EventCounters");
            return e;
        }

        public static EventAssertion<T> ArrangeEventAssertion<T>(Action<Action<T>> wireUp)
        {
            return new EventAssertion<T>(wireUp);
        }

        public class EventAssertion<T>
        {
            private Action<T> _handler;
            
            public EventAssertion(Action<Action<T>> wireUp)
            {
                _handler = e =>
                {
                    History.Add(e);
                };
                
                wireUp(_handler);
            }

            public bool Fired => History.Count > 0;
            public List<T> History { get; } = new List<T>();
            public T LastEvent => History.Last();

        }
    }
}