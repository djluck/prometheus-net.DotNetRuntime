using System;
using System.Diagnostics.Tracing;
using Prometheus.DotNetRuntime.EventListening.EventSources;
using Prometheus.DotNetRuntime.EventListening.Parsers.Util;

namespace Prometheus.DotNetRuntime.EventListening.Parsers
{
    public class KestrelEventParser : IEventParser<KestrelEventParser>, KestrelEventParser.Events.Info
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
        // TODO: add eventPairTimer for Tls events
        private readonly EventPairTimer<long> _eventPairTimerConnections;
        private readonly EventPairTimer<long> _eventPairTimerRequests;

        public event Action<Events.ConnectionStartEvent> ConnectionStart;
        public event Action<Events.ConnectionStopEvent> ConnectionStop;
        public event Action<Events.RequestStartEvent> RequestStart;
        public event Action<Events.RequestStopEvent> RequestStop;
        public event Action<Events.ConnectionRejectedEvent> ConnectionRejected;

        public Guid EventSourceGuid => KestrelEventSource.Id;
        public EventKeywords Keywords => EventKeywords.None;

        public KestrelEventParser(SamplingRate samplingRate)
        {
            _samplingRate = samplingRate;
            _eventPairTimerConnections = new EventPairTimer<long>(
                EventIdConnectionStart,
                EventIdConnectionStop,
                x => x.OSThreadId,
                samplingRate
            );
            _eventPairTimerRequests = new EventPairTimer<long>(
                EventIdRequestStart,
                EventIdRequestStop,
                x => x.OSThreadId,
                samplingRate
            );
        }

        public void ProcessEvent(EventWrittenEventArgs e)
        {
            switch (_eventPairTimerConnections.TryGetDuration(e, out var duration1))
            {
                case DurationResult.Start:
                    ConnectionStart?.Invoke(Events.ConnectionStartEvent.Instance);
                    break;

                case DurationResult.FinalWithDuration:
                    ConnectionStop?.InvokeManyTimes(_samplingRate.SampleEvery, Events.ConnectionStopEvent.GetFrom(duration1));
                    break;

                default:
                    break;
            }

            switch (_eventPairTimerRequests.TryGetDuration(e, out var duration2))
            {
                case DurationResult.Start:
                    ConnectionStart?.Invoke(Events.ConnectionStartEvent.Instance);
                    break;

                case DurationResult.FinalWithDuration:
                    ConnectionStop?.InvokeManyTimes(_samplingRate.SampleEvery, Events.ConnectionStopEvent.GetFrom(duration2));
                    break;

                default:
                    break;
            }

            if (e.EventId == EventIdConnectionStart)
            {
                ConnectionStart?.Invoke(Events.ConnectionStartEvent.ParseFrom(e));
                return;
            }

            if (e.EventId == EventIdConnectionStop)
            {
                ConnectionStop?.Invoke(Events.ConnectionStopEvent.ParseFrom(e));
                return;
            }

            if (e.EventId == EventIdRequestStart)
            {
                RequestStart?.Invoke(Events.RequestStartEvent.ParseFrom(e));
                return;
            }

            if (e.EventId == EventIdRequestStop)
            {
                RequestStop?.Invoke(Events.RequestStopEvent.ParseFrom(e));
                return;
            }

            if (e.EventId == EventIdConnectionRejected)
            {
                ConnectionRejected?.Invoke(Events.ConnectionRejectedEvent.ParseFrom(e));
                return;
            }
        }

        public static class Events
        {
            public interface Info : IInfoEvents
            {
                event Action<ConnectionStartEvent> ConnectionStart;
                event Action<ConnectionStopEvent> ConnectionStop;
                event Action<RequestStartEvent> RequestStart;
                event Action<RequestStopEvent> RequestStop;
                event Action<ConnectionRejectedEvent> ConnectionRejected;
            }
            
            public class ConnectionStartEvent
            {
                public static readonly ConnectionStartEvent Instance = new();
                private ConnectionStartEvent() { }

                //public string ConnectionId { get; private set; }
                //public string LocalEndPoint { get; private set; }
                //public string RemoteEndPoint { get; private set; }

                public static ConnectionStartEvent ParseFrom(EventWrittenEventArgs e)
                {
                    //Instance.ConnectionId = (string)e.Payload[0];
                    //Instance.LocalEndPoint = (string)e.Payload[1];
                    //Instance.RemoteEndPoint = (string)e.Payload[2];
                    return Instance;
                }

            }

            public class ConnectionStopEvent
            {
                private static readonly ConnectionStopEvent Instance = new();

                private ConnectionStopEvent()
                {
                }

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

                public static ConnectionStopEvent ParseFrom(EventWrittenEventArgs e)
                {
                    //Instance.ConnectionId = (string)e.Payload[0];
                    //Instance.LocalEndPoint = (string)e.Payload[1];
                    //Instance.RemoteEndPoint = (string)e.Payload[2];
                    return Instance;
                }
            }

            public class RequestStartEvent
            {
                private static readonly RequestStartEvent Instance = new();
                private RequestStartEvent() { }

                public static RequestStartEvent ParseFrom(EventWrittenEventArgs e)
                {
                    return Instance;
                }
            }

            public class RequestStopEvent
            {
                private static readonly RequestStopEvent Instance = new();
                private RequestStopEvent() { }

                public TimeSpan RequestDuration { get; private set; }

                public static RequestStopEvent GetFrom(TimeSpan requestDuration)
                {
                    Instance.RequestDuration = requestDuration;
                    return Instance;
                }

                public static RequestStopEvent ParseFrom(EventWrittenEventArgs e)
                {
                    return Instance;
                }
            }
            
            public class ConnectionRejectedEvent
            {
                private static readonly ConnectionRejectedEvent Instance = new();
                private ConnectionRejectedEvent() { }

                public static ConnectionRejectedEvent ParseFrom(EventWrittenEventArgs e)
                {
                    return Instance;
                }
            }
        }
    }
}
