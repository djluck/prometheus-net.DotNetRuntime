using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
#if PROMV2
using Prometheus.Advanced;
#endif
using Prometheus.DotNetRuntime.EventSources;
using Prometheus.DotNetRuntime.StatsCollectors.Util;

namespace Prometheus.DotNetRuntime.StatsCollectors
{
    /// <summary>
    /// Measures the activity of the JIT (Just In Time) compiler in a process.
    /// Tracks how often it runs and how long it takes to compile methods
    /// </summary>
    internal sealed class JitStatsCollector : IEventSourceStatsCollector
    {
        private readonly SamplingRate _samplingRate;
        private const int EventIdMethodJittingStarted = 145, EventIdMethodLoadVerbose = 143;
        private const string DynamicLabel = "dynamic";
        private const string LabelValueTrue = "true";
        private const string LabelValueFalse = "false";

        private readonly EventPairTimer<ulong> _eventPairTimer;

        private readonly Ratio _jitCpuRatio = Ratio.ProcessTotalCpu();

        public JitStatsCollector(SamplingRate samplingRate)
        {
            _samplingRate = samplingRate;
            _eventPairTimer = new EventPairTimer<ulong>(
                EventIdMethodJittingStarted,
                EventIdMethodLoadVerbose,
                x => (ulong)x.Payload[0],
                samplingRate
            );
        }
       
        public EventKeywords Keywords => (EventKeywords) DotNetRuntimeEventSource.Keywords.Jit;
        public EventLevel Level => EventLevel.Verbose;
        public Guid EventSourceGuid => DotNetRuntimeEventSource.Id;

        internal Counter MethodsJittedTotal { get; private set; }
        internal Counter MethodsJittedSecondsTotal { get; private set; }
        internal Gauge CpuRatio { get; private set; }

        public void RegisterMetrics(MetricFactory metrics)
        {
            MethodsJittedTotal = metrics.CreateCounter("dotnet_jit_method_total", "Total number of methods compiled by the JIT compiler", DynamicLabel);
            MethodsJittedSecondsTotal = metrics.CreateCounter("dotnet_jit_method_seconds_total", "Total number of seconds spent in the JIT compiler", DynamicLabel);
            CpuRatio = metrics.CreateGauge("dotnet_jit_cpu_ratio", "The amount of total CPU time consumed spent JIT'ing");
        }

        public void UpdateMetrics()
        {
            CpuRatio.Set(_jitCpuRatio.CalculateConsumedRatio(MethodsJittedSecondsTotal));
        }

        public void ProcessEvent(EventWrittenEventArgs e)
        {
            if (_eventPairTimer.TryGetDuration(e, out var duration) == DurationResult.FinalWithDuration)
            {
                // dynamic methods are of special interest to us- only a certain number of JIT'd dynamic methods
                // will be cached. Frequent use of dynamic can cause methods to be evicted from the cache and re-JIT'd
                var methodFlags = (uint)e.Payload[5];
                var dynamicLabelValue = (methodFlags & 0x1) == 0x1 ? LabelValueTrue : LabelValueFalse;
                
                MethodsJittedTotal.Labels(dynamicLabelValue).Inc(_samplingRate.SampleEvery);
                MethodsJittedSecondsTotal.Labels(dynamicLabelValue).Inc(duration.TotalSeconds * _samplingRate.SampleEvery);
            }
        }
    }
}