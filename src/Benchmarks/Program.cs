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
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;
using Benchmarks.Benchmarks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Perfolizer.Horology;
using Perfolizer.Mathematics.OutlierDetection;
using Prometheus.DotNetRuntime;

namespace Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<EventCounterParserBenchmark>(
                DefaultConfig.Instance
                    .With(
                        new Job()
                            .With(RunStrategy.Throughput)
                            .WithWarmupCount(1)
                            .WithIterationTime(TimeInterval.FromMilliseconds(300))
                            .WithMaxIterationCount(30)
                            .WithCustomBuildConfiguration("Release")
                            .WithOutlierMode(OutlierMode.DontRemove)
                    )
                    .With(MemoryDiagnoser.Default)
            );
            // BenchmarkSwitcher.FromTypes(new []{typeof(BaselineBenchmark), typeof(NoSamplingBenchmark), typeof(DefaultBenchmark)}).RunAllJoined(
            //     DefaultConfig.Instance
            //         .With(
            //             new Job()
            //                 .With(RunStrategy.Monitoring)
            //                 .WithLaunchCount(3)
            //                 .WithWarmupCount(1)
            //                 .WithIterationTime(TimeInterval.FromSeconds(10))
            //                 .WithCustomBuildConfiguration("Release")
            //                 .WithOutlierMode(OutlierMode.DontRemove)
            //         )
            //         .With(MemoryDiagnoser.Default)
            //         .With(HardwareCounter.TotalCycles)
            // );
        }
    }
}