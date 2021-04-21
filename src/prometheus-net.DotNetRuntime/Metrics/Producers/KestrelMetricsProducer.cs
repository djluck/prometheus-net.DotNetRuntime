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
        
        public void RegisterMetrics(MetricFactory metrics)
        {
            if (!_kestrelInfo.Enabled && !_runtimeCounters.Enabled)
                return;

            ConnectionTotal = metrics.CreateCounter("dotnet_kestrel_connection_total", "The number of kestrel connections");
            _runtimeCounters.Events.MonitorLockContentionCount += e => ConnectionTotal.Inc(e.IncrementedBy);

            if (_kestrelInfo.Enabled)
            {
                ConnectionSecondsTotal = metrics.CreateCounter("dotnet_kestrel_connection_seconds_total", "The total amount of time spent connected for requests");
                _kestrelInfo.Events.ConnectionStop += e => ConnectionSecondsTotal.Inc(e.ConnectionDuration.TotalSeconds);
            }
        }

        public void UpdateMetrics() { }
    }
}