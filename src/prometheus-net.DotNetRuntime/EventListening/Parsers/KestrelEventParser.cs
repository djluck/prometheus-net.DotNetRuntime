using System;
using System.Diagnostics.Tracing;
using Prometheus.DotNetRuntime.EventListening.EventSources;
using Prometheus.DotNetRuntime.EventListening.Parsers.Util;

namespace Prometheus.DotNetRuntime.EventListening.Parsers
{
    public class KestrelEventParser : IEventParser<KestrelEventParser>, KestrelEventParser.Events.Info, KestrelEventParser.Events.Verbose
    {
        private readonly SamplingRate _samplingRate;
        private const int
            EventIdConnectionStart = 1,
            EventIdConnectionStop = 2,
            EventIdRequestStart = 3,
            EventIdRequestStop = 4,
            EventIdConnectionRejected = 5,
            EventIdTlsHandshakeStart = 8,
            EventIdTlsHandshakeStop = 9,
            EventIdTlsHandshakeFailed = 10;
        private readonly EventPairTimer<long> _eventPairTimer;

        public event Action<Events.ConnectionStartEvent> ConnectionStart;
        public event Action<Events.ConnectionStopEvent> ConnectionStop;

        public Guid EventSourceGuid => KestrelEventSource.Id;
        public EventKeywords Keywords => EventKeywords.None;

        public KestrelEventParser(SamplingRate samplingRate)
        {
            _samplingRate = samplingRate;
            _eventPairTimer = new EventPairTimer<long>(
                EventIdConnectionStart,
                EventIdConnectionStop,
                x => x.OSThreadId,
                samplingRate
            );
        }

        public void ProcessEvent(EventWrittenEventArgs e)
        {
            switch (_eventPairTimer.TryGetDuration(e, out var duration))
            {
                case DurationResult.Start:
                    ConnectionStart?.Invoke(Events.ConnectionStartEvent.Instance);
                    return;

                case DurationResult.FinalWithDuration:
                    ConnectionStop?.InvokeManyTimes(_samplingRate.SampleEvery, Events.ConnectionStopEvent.GetFrom(duration));
                    return;

                default:
                    return;
            }
        }

        public static class Events
        {
            public interface Info : IInfoEvents
            {
                event Action<ConnectionStartEvent> ConnectionStart;
                event Action<ConnectionStopEvent> ConnectionStop;
            }

            public interface Verbose : IVerboseEvents
            {
                
            }

            public class ConnectionStartEvent
            {
                public static readonly ConnectionStartEvent Instance = new();
                private ConnectionStartEvent() { }

                //public string ConnectionId { get; private set; }
                //public string LocalEndPoint { get; private set; }
                //public string RemoteEndPoint { get; private set; }

                //public static ConnectionStartEvent ParseFrom(EventWrittenEventArgs e)
                //{
                //    Instance.ConnectionId = (string)e.Payload[0];
                //    Instance.LocalEndPoint = (string)e.Payload[1];
                //    Instance.RemoteEndPoint = (string) e.Payload[2];
                //    return Instance;
                //}
            }

            public class ConnectionStopEvent
            {
                private static readonly ConnectionStopEvent Instance = new();
                private ConnectionStopEvent() { }

                //public string ConnectionId { get; private set; }
                //public string LocalEndPoint { get; private set; }
                //public string RemoteEndPoint { get; private set; }

                //public static ConnectionStopEvent ParseFrom(EventWrittenEventArgs e)
                //{
                //    Instance.ConnectionId = (string)e.Payload[0];
                //    Instance.LocalEndPoint = (string)e.Payload[1];
                //    Instance.RemoteEndPoint = (string)e.Payload[2];
                //    return Instance;
                //}

                public TimeSpan ConnectionDuration { get; private set; }

                public static ConnectionStopEvent GetFrom(TimeSpan connectionDuration)
                {
                    Instance.ConnectionDuration = connectionDuration;
                    return Instance;
                }
            }
        }
    }
}
