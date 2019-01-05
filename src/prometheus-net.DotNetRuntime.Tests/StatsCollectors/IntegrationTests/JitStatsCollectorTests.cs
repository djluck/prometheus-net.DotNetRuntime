using System.Runtime.CompilerServices;
using System.Threading;
using NUnit.Framework;
using Prometheus.Advanced;
using Prometheus.DotNetRuntime;
using Prometheus.DotNetRuntime.StatsCollectors;
using ProtoBuf.Meta;

namespace Prometheus.DotNetRuntime.Tests.StatsCollectors.IntegrationTests
{
    internal class JitStatsCollectorTests : StatsCollectorIntegrationTestBase<JitStatsCollector>
    {
        protected override JitStatsCollector CreateStatsCollector()
        {
            return new JitStatsCollector();
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
}