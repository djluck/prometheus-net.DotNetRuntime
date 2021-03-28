using Prometheus.DotNetRuntime.EventListening.Parsers;

namespace Prometheus.DotNetRuntime.Metrics.Producers
{
    public class ContentionMetricsProducer : IMetricProducer
    {
        private readonly Consumes<ContentionEventParser.Events.Info> _contentionInfo;
        private readonly Consumes<RuntimeEventParser.Events.Counters> _runtimeCounters;

        public ContentionMetricsProducer(Consumes<ContentionEventParser.Events.Info> contentionInfo, Consumes<RuntimeEventParser.Events.Counters> runtimeCounters)
        {
            _contentionInfo = contentionInfo;
            _runtimeCounters = runtimeCounters;
        }
        
        internal Counter ContentionSecondsTotal { get; private set; }
        internal Counter ContentionTotal { get; private set; }
        
        public void RegisterMetrics(MetricFactory metrics)
        {
            if (!_contentionInfo.Enabled && !_runtimeCounters.Enabled)
                return;
            
            ContentionTotal = metrics.CreateCounter("dotnet_contention_total", "The number of locks contended");
            _runtimeCounters.Events.MonitorLockContentionCount += e => ContentionTotal.Inc(e.IncrementedBy);

            if (_contentionInfo.Enabled)
            {
                ContentionSecondsTotal = metrics.CreateCounter("dotnet_contention_seconds_total", "The total amount of time spent contending locks");
                _contentionInfo.Events.ContentionEnd += e => ContentionSecondsTotal.Inc(e.ContentionDuration.TotalSeconds);
            }
        }

        public void UpdateMetrics() { }
    }
}