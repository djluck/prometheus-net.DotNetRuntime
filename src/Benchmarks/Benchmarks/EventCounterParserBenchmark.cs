using System;
using System.Collections.ObjectModel;
using System.Diagnostics.Tracing;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Fasterflect;
using Prometheus.DotNetRuntime.EventListening;

namespace Benchmarks.Benchmarks
{
    public class EventCounterParserBenchmark
    {
        private EventWrittenEventArgs _meanValue;
        private EventWrittenEventArgs _incrCounter;
        private DummyTypeEventCounterParser _parser;

        public EventCounterParserBenchmark()
        {
            _meanValue = CreateCounterEventWrittenEventArgs(
                ("Name", "test-mean-counter"),
                ("DisplayName", "some value"),
                ("Mean", 5.0),
                ("StandardDeviation", 1),
                ("Count", 1),
                ("Min", 1),
                ("Max", 1),
                ("IntervalSec", 1),
                ("Series", 1),
                ("CounterType", "Mean"),
                ("Metadata", ""),
                ("DisplayUnits", "")
            );
            
            _incrCounter = CreateCounterEventWrittenEventArgs(
                ("Name", "test-incrementing-counter"),
                ("DisplayName", "some value"),
                ("DisplayRateTimeScale", TimeSpan.FromSeconds(10)),
                ("Increment", 6.0),
                ("IntervalSec", 1),
                ("Series", 1),
                ("CounterType", "Sum"),
                ("Metadata", ""),
                ("DisplayUnits", "")
            );

            _parser = new DummyTypeEventCounterParser();
            var total = 0.0;
            _parser.TestIncrementingCounter += e => total += e.IncrementedBy;

            var last = 0.0;
            _parser.TestMeanCounter += e => last = e.Mean;
        }
        
        [Benchmark]
        public void ParseIncrementingCounter()
        {
            _parser.ProcessEvent(_incrCounter);
        }
        
        [Benchmark]
        public void ParseMeanCounter()
        {
            _parser.ProcessEvent(_meanValue);
        }
        
        public static EventWrittenEventArgs CreateEventWrittenEventArgs(int eventId, DateTime? timestamp = null, params object[] payload)
        {
            var args = (EventWrittenEventArgs)typeof(EventWrittenEventArgs).CreateInstance(new []{ typeof(EventSource)}, Flags.NonPublic | Flags.Instance, new object[] { null});
            args.SetPropertyValue("EventId", eventId);
            args.SetPropertyValue("Payload", new ReadOnlyCollection<object>(payload));
            
            if (timestamp.HasValue)
            {
                args.SetPropertyValue("TimeStamp", timestamp.Value);
            }
            
            return args;
        }
        
        public static EventWrittenEventArgs CreateCounterEventWrittenEventArgs(params (string key, object val)[] payload)
        {
            var counterPayload = payload.ToDictionary(k => k.key, v => v.val);

            var e = CreateEventWrittenEventArgs(-1, DateTime.UtcNow, new[] { counterPayload });
            e.SetPropertyValue("EventName", "EventCounters");
            return e;
        }
        
        public interface TestCounters : ICounterEvents
        {
#pragma disable warning
            public event Action<IncrementingCounterValue> TestIncrementingCounter;
            public event Action<MeanCounterValue> TestMeanCounter;
#pragma warning restore
        }
        
        public class DummyTypeEventCounterParser : EventCounterParserBase<DummyTypeEventCounterParser>, TestCounters
        {
            public override string EventSourceName { get; }
            public override EventKeywords Keywords { get; }
            public override int RefreshIntervalSeconds { get; set; }
            
            [CounterName("test-incrementing-counter")]
            public event Action<IncrementingCounterValue> TestIncrementingCounter;
            [CounterName("test-mean-counter")]
            public event Action<MeanCounterValue> TestMeanCounter;
        }
    }
}