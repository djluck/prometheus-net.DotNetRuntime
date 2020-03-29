using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Prometheus.DotNetRuntime;

namespace Benchmarks.Benchmarks
{
    public abstract class DotNetRuntimeStatsBenchmarkBase : AspNetBenchmarkBase
    {
        private IDisposable collector;

        protected override void PreGlobalSetup()
        {
            collector = GetStatsBuilder().StartCollecting();
        }

        protected override void PostGlobalCleanup()
        {
            collector.Dispose();
        }

        [Benchmark(Baseline = false, OperationsPerInvoke = NumRequests)]
        public async Task Make_Requests()
        {
            await MakeHttpRequests();
        }

        protected abstract DotNetRuntimeStatsBuilder.Builder GetStatsBuilder();
    }
}