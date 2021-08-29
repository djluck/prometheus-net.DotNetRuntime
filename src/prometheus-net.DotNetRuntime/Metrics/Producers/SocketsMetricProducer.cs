using Prometheus.DotNetRuntime.EventListening.Parsers;

namespace Prometheus.DotNetRuntime.Metrics.Producers
{
    public class SocketsMetricProducer : IMetricProducer
    {
        private readonly Consumes<SocketsEventParser.Events.CountersV5_0> _socketCounters;

        public SocketsMetricProducer(Consumes<SocketsEventParser.Events.CountersV5_0> socketCounters)
        {
            _socketCounters = socketCounters;
        }
        
        public void RegisterMetrics(MetricFactory metrics)
        {
            if (!_socketCounters.Enabled)
                return;

            OutgoingConnectionEstablished = metrics.CreateCounter("dotnet_sockets_connections_established_outgoing_total", "The total number of outgoing established TCP connections");
            var lastEstablishedOutgoing = 0.0;
            _socketCounters.Events.OutgoingConnectionsEstablished += e =>
            {
                OutgoingConnectionEstablished.Inc(e.Mean - lastEstablishedOutgoing);
                lastEstablishedOutgoing = e.Mean;
            };
            
            IncomingConnectionEstablished = metrics.CreateCounter("dotnet_sockets_connections_established_incoming_total", "The total number of incoming established TCP connections");
            var lastEstablishedIncoming = 0.0;
            _socketCounters.Events.IncomingConnectionsEstablished += e =>
            {
                IncomingConnectionEstablished.Inc(e.Mean - lastEstablishedIncoming);
                lastEstablishedIncoming = e.Mean;
            };
            
            BytesReceived = metrics.CreateCounter("dotnet_sockets_bytes_received_total", "The total number of bytes received over the network");
            var lastReceived = 0.0;
            _socketCounters.Events.BytesReceived += e =>
            {
                BytesReceived.Inc(e.Mean - lastReceived);
                lastReceived = e.Mean;
            };
            
            var lastSent = 0.0;
            BytesSent = metrics.CreateCounter("dotnet_sockets_bytes_sent_total", "The total number of bytes sent over the network");
            _socketCounters.Events.BytesSent += e =>
            {
                BytesSent.Inc(e.Mean - lastSent);
                lastSent = e.Mean;
            };
        }

        internal Counter BytesSent { get; private set; }
        internal Counter BytesReceived { get; private set; }
        internal Counter IncomingConnectionEstablished { get; private set; }
        internal Counter OutgoingConnectionEstablished { get; private set; }

        public void UpdateMetrics()
        {
        }
    }
}