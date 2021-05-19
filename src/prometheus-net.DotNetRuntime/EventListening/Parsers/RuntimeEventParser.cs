using System;
using System.Diagnostics.Tracing;
using Prometheus.DotNetRuntime.EventListening;

#nullable enable

namespace Prometheus.DotNetRuntime.EventListening.Parsers
{
    public class RuntimeEventParser : EventCounterParserBase<RuntimeEventParser>,
        RuntimeEventParser.Events.CountersV3_0,
        RuntimeEventParser.Events.CountersV5_0
    {
#pragma warning disable CS0067
        [CounterName("threadpool-thread-count")]
        public event Action<MeanCounterValue>? ConnectionCount;
            
        [CounterName("threadpool-queue-length")]
        public event Action<MeanCounterValue>? ThreadPoolQueueLength;
            
        [CounterName("threadpool-completed-items-count")]
        public event Action<IncrementingCounterValue>? ConnectionCompletedItemsCount;

        [CounterName("monitor-lock-contention-count")]
        public event Action<IncrementingCounterValue>? MonitorLockContentionCount;

        [CounterName("active-timer-count")] 
        public event Action<MeanCounterValue>? ActiveTimerCount;
            
        [CounterName("exception-count")]
        public event Action<IncrementingCounterValue>? ExceptionCount;
            
        [CounterName("assembly-count")]
        public event Action<MeanCounterValue>? NumAssembliesLoaded;

        [CounterName("il-bytes-jitted")] 
        public event Action<MeanCounterValue>? IlBytesJitted;

        [CounterName("methods-jitted-count")] 
        public event Action<MeanCounterValue>? MethodsJittedCount;

        [CounterName("alloc-rate")] 
        public event Action<IncrementingCounterValue>? AllocRate;
        
        [CounterName("gc-heap-size")]
        public event Action<MeanCounterValue>? GcHeapSize;
        [CounterName("gen-0-gc-count")]
        public event Action<IncrementingCounterValue>? Gen0GcCount;
        [CounterName("gen-1-gc-count")]
        public event Action<IncrementingCounterValue>? Gen1GcCount;
        [CounterName("gen-2-gc-count")]
        public event Action<IncrementingCounterValue>? Gen2GcCount;
        [CounterName("time-in-gc")]
        public event Action<MeanCounterValue>? TimeInGc;
        [CounterName("gen-0-size")]
        public event Action<MeanCounterValue>? Gen0Size;
        [CounterName("gen-1-size")]
        public event Action<MeanCounterValue>? Gen1Size;
        [CounterName("gen-2-size")]
        public event Action<MeanCounterValue>? Gen2Size;
        [CounterName("loh-size")]
        public event Action<MeanCounterValue>? LohSize;
#pragma warning restore CS0067

        public override Guid EventSourceGuid => EventSources.SystemRuntimeEventSource.Id;
        public override EventKeywords Keywords { get; }
        public override int RefreshIntervalSeconds { get; set; } = 1;
        
        public static class Events
        {
            public interface CountersV3_0 : ICounterEvents
            {
                event Action<MeanCounterValue> ConnectionCount;
                event Action<MeanCounterValue> ThreadPoolQueueLength;
                event Action<IncrementingCounterValue> ConnectionCompletedItemsCount; 
                event Action<IncrementingCounterValue> MonitorLockContentionCount;
                event Action<MeanCounterValue> ActiveTimerCount;
                event Action<IncrementingCounterValue> ExceptionCount;
                event Action<MeanCounterValue> NumAssembliesLoaded;
                event Action<IncrementingCounterValue> AllocRate;
                event Action<MeanCounterValue> GcHeapSize;
                event Action<IncrementingCounterValue> Gen0GcCount;
                event Action<IncrementingCounterValue> Gen1GcCount;
                event Action<IncrementingCounterValue> Gen2GcCount;
                event Action<MeanCounterValue> TimeInGc;
                event Action<MeanCounterValue> Gen0Size;
                event Action<MeanCounterValue> Gen1Size;
                event Action<MeanCounterValue> Gen2Size;
                event Action<MeanCounterValue> LohSize;
            }
            
            public interface CountersV5_0 : ICounterEvents
            {
                event Action<MeanCounterValue> IlBytesJitted;
                event Action<MeanCounterValue> MethodsJittedCount;
            }
        }
    }
}