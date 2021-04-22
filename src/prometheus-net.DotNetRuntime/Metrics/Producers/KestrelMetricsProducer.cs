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

        internal Counter CurrentConnectionCount { get; private set; }
        internal Counter ConnectionSecondsTotal { get; private set; }
        internal Counter ConnectionTotal { get; private set; }
        internal Gauge NumConnectionThreads { get; private set; }
        internal Gauge NumIocThreads { get; private set; }
        internal Counter AdjustmentsTotal { get; private set; }
        internal Counter Throughput { get; private set; }
        internal Histogram QueueLength { get; private set; }
        internal Gauge NumTimers { get; private set; }

        public void RegisterMetrics(MetricFactory metrics)
        {
            if (!_kestrelInfo.Enabled && !_runtimeCounters.Enabled)
                return;

            ConnectionTotal = metrics.CreateCounter("dotnet_kestrel_total_connections", "The total number of kestrel connections");
            NumConnectionThreads = metrics.CreateGauge("dotnet_kestrel_current_connections",
                "The current number of kestrel connections");
            _kestrelInfo.Events.ConnectionStart += e => ConnectionTotal.Inc();
            _kestrelInfo.Events.ConnectionStart += e => NumConnectionThreads.Inc();

            //ConnectionSecondsTotal = metrics.CreateCounter("dotnet_kestrel_connection_seconds_total", "The total amount of time spent connected for requests");
            //_kestrelInfo.Events.ConnectionStop += e => ConnectionSecondsTotal.Inc(e.ConnectionDuration.TotalSeconds);
            _kestrelInfo.Events.ConnectionStop += e => NumConnectionThreads.Dec();
        }

        public void UpdateMetrics() { }
    }
}