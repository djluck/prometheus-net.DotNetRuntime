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

            OutgoingConnections = metrics.CreateGauge("dotnet_sockets_connections_outgoing_count", "The current number of outgoing established TCP connections");
            _socketCounters.Events.OutgoingConnectionsEstablished += e => OutgoingConnections.Set(e.Mean);
            
            IncomingConnections = metrics.CreateGauge("dotnet_sockets_connections_incoming_count", "The current number of incoming established TCP connections");
            _socketCounters.Events.IncomingConnectionsEstablished += e => IncomingConnections.Set(e.Mean);
            
            BytesReceived = metrics.CreateGauge("dotnet_sockets_bytes_received_total", "The total number of bytes received over the network");
            _socketCounters.Events.BytesReceived += e => BytesReceived.Inc(e.Mean);
            
            BytesSent = metrics.CreateGauge("dotnet_sockets_bytes_sent_total", "The total number of bytes sent over the network");
            _socketCounters.Events.BytesSent += e => BytesSent.Inc(e.Mean);
        }

        internal Gauge BytesSent { get; private set; }
        internal Gauge BytesReceived { get; private set; }
        internal Gauge IncomingConnections { get; private set; }
        internal Gauge OutgoingConnections { get; private set; }

        public void UpdateMetrics()
        {
        }
    }
}