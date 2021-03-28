using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Channels;
using BenchmarkDotNet.Attributes;

namespace Benchmarks.Benchmarks
{
    public class DictBenchmark
    {
        private Dictionary<int, string> _dict ;
        private ConcurrentDictionary<int, string> _connDict;
        
        public DictBenchmark()
        {
        }

        [IterationSetup]
        public void Setup()
        {
            _dict = new Dictionary<int, string>(12000);
            _connDict = new (concurrencyLevel: Environment.ProcessorCount, 20000);

        }

        [Benchmark]
        public void AddToDict()
        {
            for (int i = 0; i < 10000; i++)
                _dict.Add(i, "test value");
        }
        
        [Benchmark]
        public void AddToConcurrentDict()
        {
            for (int i = 0; i < 10000; i++)
                _connDict.TryAdd(i, "test value");
        }
    }
}