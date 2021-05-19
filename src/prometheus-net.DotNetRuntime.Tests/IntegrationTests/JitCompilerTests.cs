using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using Prometheus.DotNetRuntime.Metrics.Producers;


namespace Prometheus.DotNetRuntime.Tests.IntegrationTests
{
    internal class Given_A_JitStatsCollector_That_Samples_Every_Jit_Event : IntegrationTestBase<JitMetricsProducer>
    {
        protected override DotNetRuntimeStatsBuilder.Builder ConfigureBuilder(DotNetRuntimeStatsBuilder.Builder toConfigure)
        {
            return toConfigure.WithJitStats(CaptureLevel.Verbose, SampleEvery.OneEvent);
        }

        [Test]
        public void When_a_method_is_jitted_then_its_compilation_is_measured()
        {
            // arrange
            var methodsJitted = MetricProducer.MethodsJittedTotal.Labels("false").Value;
            var methodsJittedSeconds = MetricProducer.MethodsJittedSecondsTotal.Labels("false").Value;
            
            // act (call a method, JIT'ing it)
            ToJit();
            
            // assert
            Assert.That(() => MetricProducer.MethodsJittedTotal.Labels("false").Value, Is.GreaterThanOrEqualTo(methodsJitted  + 1).After(100, 10));
            Assert.That(MetricProducer.MethodsJittedSecondsTotal.Labels("false").Value, Is.GreaterThan(methodsJittedSeconds ));
        }
        
        [Test]
        public void When_a_method_is_jitted_then_the_CPU_ratio_can_be_measured()
        {
            // act (call a method, JIT'ing it)
            ToJit();
            MetricProducer.UpdateMetrics();
            
            // assert
            Assert.That(() => MetricProducer.CpuRatio.Value, Is.GreaterThanOrEqualTo(0.0).After(100, 10));
        }
        
        [Test]
        public void When_a_dynamic_method_is_jitted_then_its_compilation_is_measured()
        {
            // arrange
            var dynamicMethodsJitted = MetricProducer.MethodsJittedTotal.Labels("true").Value;
            var dynamicMethodsJittedSeconds = MetricProducer.MethodsJittedSecondsTotal.Labels("true").Value;
            
            // act (call a method, JIT'ing it)
            ToJitDynamic();
            
            // assert
            Assert.That(() => MetricProducer.MethodsJittedTotal.Labels("true").Value, Is.GreaterThanOrEqualTo(dynamicMethodsJitted + 1).After(100, 10));
            Assert.That(MetricProducer.MethodsJittedSecondsTotal.Labels("true").Value, Is.GreaterThan(dynamicMethodsJittedSeconds ));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int ToJit()
        {
            return 1;
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private int ToJitDynamic()
        {
            dynamic o = "string";
            return o.Length;
        }
    }
    
    internal class Given_A_JitStatsCollector_That_Samples_Every_Fifth_Jit_Event : IntegrationTestBase<JitMetricsProducer>
    {
        protected override DotNetRuntimeStatsBuilder.Builder ConfigureBuilder(DotNetRuntimeStatsBuilder.Builder toConfigure)
        {
            return toConfigure.WithJitStats(CaptureLevel.Verbose, SampleEvery.FiveEvents);
        }

        [Test]
        public void When_many_methods_are_jitted_then_their_compilation_is_measured()
        {
            // arrange
            var methodsJitted = MetricProducer.MethodsJittedTotal.Labels("true").Value;
            var methodsJittedSeconds = MetricProducer.MethodsJittedSecondsTotal.Labels("true").Value;
            
            // act
            var sp = Stopwatch.StartNew();
            RuntimeEventHelper.CompileMethods(() => 1, 100);
            sp.Stop();
            
            // assert
            Assert.That(() => MetricProducer.MethodsJittedTotal.Labels("true").Value, Is.GreaterThanOrEqualTo(methodsJitted + 20).After(100, 10));
            Assert.That(MetricProducer.MethodsJittedSecondsTotal.Labels("true").Value, Is.GreaterThan(methodsJittedSeconds + sp.Elapsed.TotalSeconds).Within(0.1));
        }
    }

    internal class Given_Only_Counters_Are_Enabled_For_JitStats : IntegrationTestBase<JitMetricsProducer>
    {
        protected override DotNetRuntimeStatsBuilder.Builder ConfigureBuilder(DotNetRuntimeStatsBuilder.Builder toConfigure)
        {
            return toConfigure.WithJitStats(CaptureLevel.Counters, SampleEvery.OneEvent);
        }

#if NET5_0
        
        [Test]
        public void When_Running_On_NET50_Then_Counts_Of_Methods_Are_Recorded()
        {
            // arrage
            var methodsJittedPrevious = MetricProducer.MethodsJittedTotal.Value;
            var bytesJittedPrevious = MetricProducer.BytesJitted.Value;
            
            // act
            RuntimeEventHelper.CompileMethods(() => 1, 100);
            
            Assert.That(MetricProducer.BytesJitted, Is.Not.Null);
            Assert.That(MetricProducer.MethodsJittedTotal, Is.Not.Null);
            Assert.That(() => MetricProducer.BytesJitted.Value, Is.GreaterThan(bytesJittedPrevious).After(2_000, 10));
            Assert.That(() => MetricProducer.MethodsJittedTotal.Value, Is.GreaterThan(methodsJittedPrevious + 100).After(2_000, 10));
        }
#endif
        
#if NETCOREAPP3_1

        [Test]
        public void When_Running_On_NETCOREAPP31_Then_No_Metrics_Are_Available()
        {
            Assert.That(MetricProducer.BytesJitted, Is.Null);
            Assert.That(MetricProducer.MethodsJittedTotal, Is.Null);
        }
#endif
    }
}