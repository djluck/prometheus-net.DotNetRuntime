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
        private readonly Ratio _jitCpuRatio = Ratio.ProcessTotalCpu();

        public JitMetricsProducer(Consumes<JitEventParser.Events.Verbose> jitVerbose)
        {
            _jitVerbose = jitVerbose;
        }
        
        internal Counter MethodsJittedTotal { get; private set; }
        internal Counter MethodsJittedSecondsTotal { get; private set; }
        internal Gauge CpuRatio { get; private set; }
        
        public void RegisterMetrics(MetricFactory metrics)
        {
            if (!_jitVerbose.Enabled)
                return;
            
            MethodsJittedTotal = metrics.CreateCounter("dotnet_jit_method_total", "Total number of methods compiled by the JIT compiler", DynamicLabel);
            MethodsJittedSecondsTotal = metrics.CreateCounter("dotnet_jit_method_seconds_total", "Total number of seconds spent in the JIT compiler", DynamicLabel);
            _jitVerbose.Events.CompilationComplete += e =>
            {
                MethodsJittedTotal.Labels(e.IsMethodDynamic.ToLabel()).Inc();
                MethodsJittedSecondsTotal.Labels(e.IsMethodDynamic.ToLabel()).Inc(e.CompilationDuration.TotalSeconds);
            };
                
            CpuRatio = metrics.CreateGauge("dotnet_jit_cpu_ratio", "The amount of total CPU time consumed spent JIT'ing");
        }

        public void UpdateMetrics()
        {
            CpuRatio.Set(_jitCpuRatio.CalculateConsumedRatio(MethodsJittedSecondsTotal));
        }
    }
}