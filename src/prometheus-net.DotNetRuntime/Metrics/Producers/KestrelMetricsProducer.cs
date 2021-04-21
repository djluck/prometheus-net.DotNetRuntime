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

        internal Gauge NumThreads { get; private set; }
        internal Counter Throughput { get; private set; }

        public void RegisterMetrics(MetricFactory metrics)
        {
            if (!_kestrelInfo.Enabled && !_runtimeCounters.Enabled)
                return;

            NumThreads = metrics.CreateGauge("dotnet_num_connections", "The number of active connections");
            _runtimeCounters.Events.ConnectionCount += e => NumThreads.Set(e.Count);

            Throughput = metrics.CreateCounter("dotnet_connections_throughput_total", "The total number of connections that have been made");
            _runtimeCounters.Events.ConnectionCompletedItemsCount += e => Throughput.Inc(e.IncrementedBy);
        }


        public void UpdateMetrics() { }
    }
}