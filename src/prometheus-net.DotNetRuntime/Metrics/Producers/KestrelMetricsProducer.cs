using Prometheus.DotNetRuntime.EventListening.EventSources;
using Prometheus.DotNetRuntime.EventListening.Parsers;
using Prometheus.DotNetRuntime.Metrics.Producers.Util;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Prometheus.DotNetRuntime.Metrics.Producers
{
    public class KestrelMetricsProducer : IMetricProducer
    {
        private readonly Consumes<KestrelEventParser.Events.Info> _kestrelInfo;
        private readonly Consumes<RuntimeEventParser.Events.Counters> _runtimeCounters;

        public KestrelMetricsProducer(Consumes<KestrelEventParser.Events.Info> kestrelInfo, Consumes<RuntimeEventParser.Events.Counters> runtimeCounters)
        {
            _kestrelInfo = kestrelInfo;
            _runtimeCounters = runtimeCounters;
        }

        internal Counter ConnectionSecondsTotal { get; private set; }
        internal Counter ConnectionTotal { get; private set; }
        internal Gauge CurrentConnectionCount { get; private set; }
        internal Counter RequestDurationSeconds { get; private set; }
        internal Counter RequestTotal { get; private set; }
        internal Gauge CurrentRequestCount { get; private set; }
        internal Counter RequestRejectedTotal { get; private set; }
        // TODO turn some of the totals into a histogram instead?
        internal Histogram QueueLength { get; private set; }

        public void RegisterMetrics(MetricFactory metrics)
        {
            if (!_kestrelInfo.Enabled && !_runtimeCounters.Enabled)
                return;

            ConnectionTotal = metrics.CreateCounter("dotnet_kestrel_total_connections", "The total number of kestrel connections");
            CurrentConnectionCount = metrics.CreateGauge("dotnet_kestrel_current_connections",
                "The current number of kestrel connections");
            _kestrelInfo.Events.ConnectionStart += e => ConnectionTotal.Inc();
            _kestrelInfo.Events.ConnectionStart += e => CurrentConnectionCount.Inc();

            ConnectionSecondsTotal = metrics.CreateCounter("dotnet_kestrel_connection_seconds_total", "The total amount of time spent on connections");
            _kestrelInfo.Events.ConnectionStop += e => ConnectionSecondsTotal.Inc(e.ConnectionDuration.TotalSeconds);
            _kestrelInfo.Events.ConnectionStop += e => CurrentConnectionCount.Dec();

            RequestTotal = metrics.CreateCounter("dotnet_kestrel_total_requests", "The total number of kestrel requests");
            CurrentRequestCount = metrics.CreateGauge("dotnet_kestrel_current_requests",
                "The current number of kestrel requests");
            _kestrelInfo.Events.RequestStart += e => RequestTotal.Inc();
            _kestrelInfo.Events.RequestStart += e => CurrentRequestCount.Inc();

            RequestDurationSeconds = metrics.CreateCounter("dotnet_kestrel_request_seconds_total", "The total amount of time spent connected for requests");
            _kestrelInfo.Events.RequestStop += e => RequestDurationSeconds.Inc(e.RequestDuration.TotalSeconds);
            _kestrelInfo.Events.RequestStop += e => CurrentRequestCount.Dec();

            RequestRejectedTotal = metrics.CreateCounter("dotnet_kestrel_requests_rejected_total", "The total amount of requests rejected");
            _kestrelInfo.Events.ConnectionRejected += e => RequestRejectedTotal.Inc();
        }

        public void UpdateMetrics() { }
    }
}