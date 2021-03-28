using System;
using System.Diagnostics.Tracing;
using Prometheus.DotNetRuntime.EventListening.EventSources;

namespace Prometheus.DotNetRuntime.EventListening.Parsers
{
    public class ExceptionEventParser : IEventParser<ExceptionEventParser>, ExceptionEventParser.Events.Error
    {
        public event Action<Events.ExceptionThrownEvent> ExceptionThrown;

        public Guid EventSourceGuid => DotNetRuntimeEventSource.Id;
        public EventKeywords Keywords => (EventKeywords) DotNetRuntimeEventSource.Keywords.Exception;

        public void ProcessEvent(EventWrittenEventArgs e)
        {
            const int EventIdExceptionThrown = 80;

            if (e.EventId == EventIdExceptionThrown)
            {
                ExceptionThrown?.Invoke(Events.ExceptionThrownEvent.ParseFrom(e));
            }
        }

        public static class Events
        {
            public interface Error : IErrorEvents
            {
                event Action<ExceptionThrownEvent> ExceptionThrown;
            }

            public class ExceptionThrownEvent
            {
                private static readonly ExceptionThrownEvent Instance = new();

                private ExceptionThrownEvent()
                {
                }

                public string ExceptionType { get; private set; }

                public static ExceptionThrownEvent ParseFrom(EventWrittenEventArgs e)
                {
                    Instance.ExceptionType = (string) e.Payload[0];

                    return Instance;
                }
            }
        }
    }
}