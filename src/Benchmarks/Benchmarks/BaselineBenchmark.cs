using System.Collections.Generic;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Prometheus.DotNetRuntime;

namespace Benchmarks.Benchmarks
{
    public class BaselineBenchmark : AspNetBenchmarkBase
    {
        [Benchmark(Description = "No stats collectors enabled", Baseline = true, OperationsPerInvoke = NumRequests)]
        public async Task Make_Requests()
        {
            await MakeHttpRequests();
        }
    }
}