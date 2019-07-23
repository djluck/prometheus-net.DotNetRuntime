using System;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Prometheus.DotNetRuntime;
using Prometheus.DotNetRuntime.StatsCollectors.Util;

namespace Benchmarks
{
    public class Program
    {
        static void Main(string[] args)
        {
            var p = new Prometheus.MetricServer(12203);
            p.Start();

            var collector = DotNetRuntimeStatsBuilder.Default().StartCollecting();
            

            var tasks = Enumerable.Range(1, 2_000_000)
                .Select(_ => Task.Run(() => 1))
                .ToArray();
            
            var b = new byte[1024 * 1000];
            var b2 = new byte[1024 * 1000];
            var b3 = new byte[1024 * 1000];

            Task.WaitAll(tasks);

            Console.WriteLine("Done");
            Console.ReadLine();
            
            return;
            BenchmarkRunner.Run<TestBenchmark>(
                DefaultConfig.Instance
                    .With(
                        Job.Core
                            .WithLaunchCount(1)
                            .WithIterationTime(TimeInterval.FromMilliseconds(200))
                            .With(Platform.X64)
                            .With(Jit.RyuJit)
                    )
            );
        }
     
        public class TestBenchmark
        {
            private TimeSpan t1 = TimeSpan.FromSeconds(1);
            private TimeSpan t2 = TimeSpan.FromSeconds(60);
            private long l1 = 1l;
            private long l2 = 60;
	
            [Benchmark]
            public TimeSpan TestAddition() => t1 + t2;


            [Benchmark]
            [MethodImpl(MethodImplOptions.NoOptimization)]
            public long TestAdditionLong() => l1 + l2;

            [Benchmark]
            [MethodImpl(MethodImplOptions.NoOptimization)]
            public long TestInterlockedIncLong() => Interlocked.Increment(ref l1);
	
            private EventPairTimer<int> timer = new EventPairTimer<int>(1, 2, x => x.EventId);

            private EventWrittenEventArgs eventArgs;

            public TestBenchmark()
            {
                eventArgs = (EventWrittenEventArgs)Activator.CreateInstance(typeof(EventWrittenEventArgs), BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.CreateInstance, (Binder) null, new object[] {null}, null);
            }
          
        }
    }
}