using Prometheus.DotNetRuntime;

namespace Benchmarks.Benchmarks
{
    public class DefaultBenchmark : DotNetRuntimeStatsBenchmarkBase
    {
        protected override DotNetRuntimeStatsBuilder.Builder GetStatsBuilder()
        {
            return DotNetRuntimeStatsBuilder.Default();
        }
    }
}