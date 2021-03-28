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
            return toConfigure.WithJitStats(SampleEvery.OneEvent);
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
            return toConfigure.WithJitStats(SampleEvery.FiveEvents);
        }

        [Test]
        public void When_many_methods_are_jitted_then_their_compilation_is_measured()
        {
            // arrange
            var methodsJitted = MetricProducer.MethodsJittedTotal.Labels("true").Value;
            var methodsJittedSeconds = MetricProducer.MethodsJittedSecondsTotal.Labels("true").Value;
            
            // act
            var sp = Stopwatch.StartNew();
            Compile100Methods(() => 1);
            sp.Stop();
            
            // assert
            Assert.That(() => MetricProducer.MethodsJittedTotal.Labels("true").Value, Is.GreaterThanOrEqualTo(methodsJitted + 20).After(100, 10));
            Assert.That(MetricProducer.MethodsJittedSecondsTotal.Labels("true").Value, Is.GreaterThan(methodsJittedSeconds + sp.Elapsed.TotalSeconds).Within(0.1));
        }

        private void Compile100Methods(Expression<Func<int>> toCompile)
        {
            for (int i = 0; i < 100; i++)
            {
                toCompile.Compile();
            }
        }
    }
}