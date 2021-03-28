using System;
using System.Diagnostics.Tracing;
using Prometheus.DotNetRuntime.EventListening.EventSources;

namespace Prometheus.DotNetRuntime.EventListening.Parsers
{
    public class ThreadPoolEventParser : IEventParser<ThreadPoolEventParser>, ThreadPoolEventParser.Events.Info
    {
        private const int
            EventIdThreadPoolSample = 54,
            EventIdThreadPoolAdjustment = 55,
            EventIdIoThreadCreate = 44,
            EventIdIoThreadRetire = 46,
            EventIdIoThreadUnretire = 47,
            EventIdIoThreadTerminate = 45;
        
        public event Action<Events.ThreadPoolAdjustedEvent> ThreadPoolAdjusted;
        public event Action<Events.IoThreadPoolAdjustedEvent> IoThreadPoolAdjusted;

        public Guid EventSourceGuid => DotNetRuntimeEventSource.Id;
        public EventKeywords Keywords => (EventKeywords) DotNetRuntimeEventSource.Keywords.Threading;

        public void ProcessEvent(EventWrittenEventArgs e)
        {
            switch (e.EventId)
            {
                case EventIdThreadPoolAdjustment:
                    ThreadPoolAdjusted?.Invoke(Events.ThreadPoolAdjustedEvent.ParseFrom(e));
                    return;

                case EventIdIoThreadCreate:
                case EventIdIoThreadRetire:
                case EventIdIoThreadUnretire:
                case EventIdIoThreadTerminate:
                    IoThreadPoolAdjusted?.Invoke(Events.IoThreadPoolAdjustedEvent.ParseFrom(e));
                    return;
            }
        }

        public static class Events
        {
            public interface Info : IInfoEvents
            {
                event Action<ThreadPoolAdjustedEvent> ThreadPoolAdjusted;
                event Action<IoThreadPoolAdjustedEvent> IoThreadPoolAdjusted;
            }
            
            public class ThreadPoolAdjustedEvent
            {
                private static readonly ThreadPoolAdjustedEvent Instance = new (); 
                private ThreadPoolAdjustedEvent() { }
            
                public DotNetRuntimeEventSource.ThreadAdjustmentReason AdjustmentReason { get; private set; }
                public uint NumThreads { get; private set; }

                public static ThreadPoolAdjustedEvent ParseFrom(EventWrittenEventArgs e)
                {
                    Instance.NumThreads = (uint) e.Payload[1];
                    Instance.AdjustmentReason = (DotNetRuntimeEventSource.ThreadAdjustmentReason) e.Payload[2];
                    return Instance;
                }
            }

            public class IoThreadPoolAdjustedEvent
            {
                private static readonly IoThreadPoolAdjustedEvent Instance = new ();
            
                private IoThreadPoolAdjustedEvent() { }
            
                public uint NumThreads { get; private set; }

                public static IoThreadPoolAdjustedEvent ParseFrom(EventWrittenEventArgs e)
                {
                    Instance.NumThreads = (uint) e.Payload[0];
                    return Instance;
                }
            }
        }
    }
}