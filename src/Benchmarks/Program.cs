using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;
using Benchmarks.Benchmarks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Prometheus.DotNetRuntime;

namespace Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkSwitcher.FromTypes(new []{typeof(BaselineBenchmark), typeof(NoSamplingBenchmark), typeof(DefaultBenchmark)}).RunAllJoined(
                DefaultConfig.Instance
                    .With(
                        new Job()
                            .With(RunStrategy.Monitoring)
                            .WithLaunchCount(3)
                            .WithWarmupCount(1)
                            .WithIterationTime(TimeInterval.FromSeconds(10))
                            .WithCustomBuildConfiguration("ReleaseV3")
                            .WithOutlierMode(OutlierMode.DontRemove)
                    )
                    .With(MemoryDiagnoser.Default)
                    .With(HardwareCounter.TotalCycles)
            );
        }
    }
}