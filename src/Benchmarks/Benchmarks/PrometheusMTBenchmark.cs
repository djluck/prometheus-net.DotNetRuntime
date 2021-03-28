using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Reports;
using Prometheus;

namespace Benchmarks.Benchmarks
{
    public class PrometheusMTBenchmark
    {
        private Counter _counter = Metrics.CreateCounter("test_counter", "");
        private Counter _counterLabelled1 = Metrics.CreateCounter("test_counter_labelled_1", "", "label1");
        private Counter _counterLabelled2 = Metrics.CreateCounter("test_counter_labelled_2", "", "label1", "label2");
        private Histogram _histogram = Metrics.CreateHistogram("test_histo", "");
        private Histogram _histogramLabelled1 = Metrics.CreateHistogram("test_histo_labeled1", "", "label1");
        private Histogram _histogramLabelled2 = Metrics.CreateHistogram("test_histo_labeled2", "", "label1", "label2");
        private CancellationTokenSource _ctSource;
        private Task[] _tasks;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _ctSource = new CancellationTokenSource();
            _tasks = Enumerable.Range(1, Environment.ProcessorCount - 4)
                .Select(async _ =>
                {
                    while (!_ctSource.IsCancellationRequested)
                    {
                        for (int i = 0; i < 500_000; i++)
                        {
                            IncrementUnlabeled();
                            IncrementLabeled1();
                            IncrementUnlabeled2();
                            ObserveUnlabeled();
                            ObserveLabeled1();
                            ObserveUnlabeled2();
                            ObserveHighUnlabeled();
                            ObserveHighLabeled1();
                            ObserveHighUnlabeled2();
                        }

                        await Task.Delay(1);
                    }
                })
                .ToArray();
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            _ctSource.Cancel();
        }
        
        [Benchmark]
        public void IncrementUnlabeled()
        {
            _counter.Inc(1);
        }
        
        [Benchmark]
        public void IncrementLabeled1()
        {
            _counterLabelled1.WithLabels("test_label1").Inc(1);
        }
        
        [Benchmark]
        public void IncrementUnlabeled2()
        {
            _counterLabelled2.WithLabels("test_label1", "test_label2").Inc(1);
        }
        
        [Benchmark]
        public void ObserveUnlabeled()
        {
            _histogram.Observe(0);
        }
        
        [Benchmark]
        public void ObserveLabeled1()
        {
            _histogramLabelled1.WithLabels("test_label1").Observe(0);
        }
        
        [Benchmark]
        public void ObserveUnlabeled2()
        {
            _histogramLabelled2.WithLabels("test_label1", "test_label2").Observe(0);
        }
        
        [Benchmark]
        public void ObserveHighUnlabeled()
        {
            _histogram.Observe(int.MaxValue);
        }
        
        [Benchmark]
        public void ObserveHighLabeled1()
        {
            _histogramLabelled1.WithLabels("test_label1").Observe(int.MaxValue);
        }
        
        [Benchmark]
        public void ObserveHighUnlabeled2()
        {
            _histogramLabelled2.WithLabels("test_label1", "test_label2").Observe(int.MaxValue);
        }

    }
}