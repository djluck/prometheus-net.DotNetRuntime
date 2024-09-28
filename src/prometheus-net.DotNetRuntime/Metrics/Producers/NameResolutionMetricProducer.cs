using Prometheus.DotNetRuntime.EventListening.Parsers;
using Prometheus.DotNetRuntime.Metrics.Producers.Util;

namespace Prometheus.DotNetRuntime.Metrics.Producers
{
    public class NameResolutionMetricProducer : IMetricProducer
    {
        private readonly Options _options;
        private readonly Consumes<NameResolutionEventParser.Events.CountersV5_0> _nameResolutionCounter;

        public NameResolutionMetricProducer(Options options, Consumes<NameResolutionEventParser.Events.CountersV5_0> nameResolutionCounter)
        {
            _nameResolutionCounter = nameResolutionCounter;
            _options = options;
        }
        
        public void RegisterMetrics(MetricFactory metrics)
        {
            if (!_nameResolutionCounter.Enabled)
                return;

            DnsLookups = metrics.CreateCounter("dotnet_dns_lookups_total", "The number of DNS lookups requested since the process started");
            var lastLookups = 0.0;
            _nameResolutionCounter.Events.DnsLookupsRequested += e =>
            {
                DnsLookups.Inc(e.Mean - lastLookups);
                lastLookups = e.Mean;
            };

            DnsLookupDuration = metrics.CreateHistogram("dotnet_dns_lookup_duration_avg_seconds",
                "The average time taken for a DNS lookup",
                new HistogramConfiguration
                {
                    Buckets = _options.HistogramBuckets
                });
            _nameResolutionCounter.Events.DnsLookupsDuration += e =>
            {
                // Convert milliseconds to seconds
                DnsLookupDuration.Observe(e.Mean / 1000.0);
            };
        }

        internal Histogram DnsLookupDuration { get; private set; }
        internal Counter DnsLookups { get; private set; }

        public void UpdateMetrics()
        {
        }

        public class Options
        {
            public double[] HistogramBuckets { get; set; } = Constants.DefaultHistogramBuckets;
        }
    }
}
