using Prometheus.DotNetRuntime.EventListening.Parsers;

namespace Prometheus.DotNetRuntime.Metrics.Producers
{
    public class ExceptionMetricsProducer : IMetricProducer
    {
        private readonly Consumes<ExceptionEventParser.Events.Error> _exceptionError;
        private readonly Consumes<RuntimeEventParser.Events.Counters> _runtimeCounters;
        private const string LabelType = "type";

        public ExceptionMetricsProducer(Consumes<ExceptionEventParser.Events.Error> exceptionError, Consumes<RuntimeEventParser.Events.Counters> runtimeCounters)
        {
            _exceptionError = exceptionError;
            _runtimeCounters = runtimeCounters;
        }
        
        internal Counter ExceptionCount { get; private set; }
        
        public void RegisterMetrics(MetricFactory metrics)
        {
            if (!_exceptionError.Enabled && !_runtimeCounters.Enabled)
                return;
            
            if (_exceptionError.Enabled)
            {
                ExceptionCount = metrics.CreateCounter(
                    "dotnet_exceptions_total",
                    "Count of exceptions thrown, broken down by type",
                    LabelType
                );

                _exceptionError.Events.ExceptionThrown += e => ExceptionCount.Labels(e.ExceptionType).Inc();
            }
            else if (_runtimeCounters.Enabled)
            {
                ExceptionCount = metrics.CreateCounter(
                    "dotnet_exceptions_total",
                    "Count of exceptions thrown"
                );

                _runtimeCounters.Events.ExceptionCount += e => ExceptionCount.Inc(e.IncrementedBy);
            }
        }

        public void UpdateMetrics() { }
    }
}