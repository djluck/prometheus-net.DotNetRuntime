using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using NUnit.Framework;
using Prometheus.DotNetRuntime;
using Prometheus.DotNetRuntime.StatsCollectors;


namespace Prometheus.DotNetRuntime.Tests.StatsCollectors.IntegrationTests
{
    internal class Given_A_JitStatsCollector_That_Samples_Every_Jit_Event : StatsCollectorIntegrationTestBase<JitStatsCollector>
    {
        protected override JitStatsCollector CreateStatsCollector()
        {
            return new JitStatsCollector(SampleEvery.OneEvent);
        }

        [Test]
        public void When_a_method_is_jitted_then_its_compilation_is_measured()
        {
            // arrange
            var methodsJitted = StatsCollector.MethodsJittedTotal.Labels("false").Value;
            var methodsJittedSeconds = StatsCollector.MethodsJittedSecondsTotal.Labels("false").Value;
            
            // act (call a method, JIT'ing it)
            ToJit();
            
            // assert
            Assert.That(() => StatsCollector.MethodsJittedTotal.Labels("false").Value, Is.GreaterThanOrEqualTo(methodsJitted  + 1).After(100, 10));
            Assert.That(StatsCollector.MethodsJittedSecondsTotal.Labels("false").Value, Is.GreaterThan(methodsJittedSeconds ));
        }
        
        [Test]
        public void When_a_method_is_jitted_then_the_CPU_ratio_can_be_measured()
        {
            // act (call a method, JIT'ing it)
            ToJit();
            StatsCollector.UpdateMetrics();
            
            // assert
            Assert.That(() => StatsCollector.CpuRatio.Value, Is.GreaterThanOrEqualTo(0.0).After(100, 10));
        }
        
        [Test]
        public void When_a_dynamic_method_is_jitted_then_its_compilation_is_measured()
        {
            // arrange
            var dynamicMethodsJitted = StatsCollector.MethodsJittedTotal.Labels("true").Value;
            var dynamicMethodsJittedSeconds = StatsCollector.MethodsJittedSecondsTotal.Labels("true").Value;
            
            // act (call a method, JIT'ing it)
            ToJitDynamic();
            
            // assert
            Assert.That(() => StatsCollector.MethodsJittedTotal.Labels("true").Value, Is.GreaterThanOrEqualTo(dynamicMethodsJitted + 1).After(100, 10));
            Assert.That(StatsCollector.MethodsJittedSecondsTotal.Labels("true").Value, Is.GreaterThan(dynamicMethodsJittedSeconds ));
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
    
    internal class Given_A_JitStatsCollector_That_Samples_Every_Fifth_Jit_Event : StatsCollectorIntegrationTestBase<JitStatsCollector>
    {
        protected override JitStatsCollector CreateStatsCollector()
        {
            return new JitStatsCollector(SampleEvery.FiveEvents);
        }

        [Test]
        public void When_many_methods_are_jitted_then_their_compilation_is_measured()
        {
            // arrange
            var methodsJitted = StatsCollector.MethodsJittedTotal.Labels("true").Value;
            var methodsJittedSeconds = StatsCollector.MethodsJittedSecondsTotal.Labels("true").Value;
            
            // act
            var sp = Stopwatch.StartNew();
            Compile100Methods(() => 1);
            sp.Stop();
            
            // assert
            Assert.That(() => StatsCollector.MethodsJittedTotal.Labels("true").Value, Is.GreaterThanOrEqualTo(methodsJitted + 20).After(100, 10));
            Assert.That(StatsCollector.MethodsJittedSecondsTotal.Labels("true").Value, Is.GreaterThan(methodsJittedSeconds + sp.Elapsed.TotalSeconds).Within(0.1));
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