using Prometheus.DotNetRuntime.EventListening.Parsers;
using Prometheus.DotNetRuntime.Metrics.Producers.Util;

namespace Prometheus.DotNetRuntime.Metrics.Producers
{
    public class JitMetricsProducer : IMetricProducer
    {
        private const string DynamicLabel = "dynamic";
        private const string LabelValueTrue = "true";
        private const string LabelValueFalse = "false";
        
        private readonly Consumes<JitEventParser.Events.Verbose> _jitVerbose;
        private readonly Consumes<RuntimeEventParser.Events.CountersV5_0> _runtimeCounters;
        private readonly Ratio _jitCpuRatio = Ratio.ProcessTotalCpu();

        public JitMetricsProducer(Consumes<JitEventParser.Events.Verbose> jitVerbose, Consumes<RuntimeEventParser.Events.CountersV5_0> runtimeCounters)
        {
            _jitVerbose = jitVerbose;
            _runtimeCounters = runtimeCounters;
        }
        
        internal Counter MethodsJittedTotal { get; private set; }
        internal Counter MethodsJittedSecondsTotal { get; private set; }
        internal Gauge BytesJitted { get; private set; }
        internal Gauge CpuRatio { get; private set; }
        
        public void RegisterMetrics(MetricFactory metrics)
        {
            if (!_jitVerbose.Enabled && !_runtimeCounters.Enabled)
                return;
            
            if (_runtimeCounters.Enabled)
            {
                BytesJitted = metrics.CreateGauge("dotnet_jit_il_bytes", "Total bytes of IL compiled by the JIT compiler");
                _runtimeCounters.Events.IlBytesJitted += e => BytesJitted.Set(e.Mean);
            }

            if (_jitVerbose.Enabled)
            {
                MethodsJittedTotal = metrics.CreateCounter("dotnet_jit_method_total", "Total number of methods compiled by the JIT compiler, broken down by compilation for dynamic code", DynamicLabel);
                MethodsJittedSecondsTotal = metrics.CreateCounter("dotnet_jit_method_seconds_total", "Total number of seconds spent in the JIT compiler, broken down by compilation for dynamic code", DynamicLabel);
                _jitVerbose.Events.CompilationComplete += e =>
                {
                    MethodsJittedTotal.Labels(e.IsMethodDynamic.ToLabel()).Inc();
                    MethodsJittedSecondsTotal.Labels(e.IsMethodDynamic.ToLabel()).Inc(e.CompilationDuration.TotalSeconds);
                };

                CpuRatio = metrics.CreateGauge("dotnet_jit_cpu_ratio", "The amount of total CPU time consumed spent JIT'ing");
            }
            else
            {
                MethodsJittedTotal = metrics.CreateCounter("dotnet_jit_method_total", "Total number of methods compiled by the JIT compiler");
                _runtimeCounters.Events.MethodsJittedCount += e => MethodsJittedTotal.Inc(e.Mean - MethodsJittedTotal.Value);
            }
        }

        public void UpdateMetrics()
        {
            CpuRatio?.Set(_jitCpuRatio.CalculateConsumedRatio(MethodsJittedSecondsTotal));
        }
    }
}