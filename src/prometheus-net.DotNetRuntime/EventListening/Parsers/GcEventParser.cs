using System;
using System.Diagnostics.Tracing;
using Prometheus.DotNetRuntime.EventListening.EventSources;
using Prometheus.DotNetRuntime.EventListening.Parsers.Util;

namespace Prometheus.DotNetRuntime.EventListening.Parsers
{
    public class GcEventParser : IEventParser<GcEventParser>, GcEventParser.Events.Info, GcEventParser.Events.Verbose
    {
        private const int
            EventIdGcStart = 1,
            EventIdGcStop = 2,
            EventIdSuspendEEStart = 9,
            EventIdRestartEEStop = 3,
            EventIdHeapStats = 4,
            EventIdAllocTick = 10;

        private readonly EventPairTimer<uint, GcData> _gcEventTimer = new EventPairTimer<uint, GcData>(
            EventIdGcStart,
            EventIdGcStop,
            x => (uint) x.Payload[0],
            x => new GcData((uint) x.Payload[1], (DotNetRuntimeEventSource.GCType) x.Payload[3]),
            SampleEvery.OneEvent);

        private readonly EventPairTimer<int> _gcPauseEventTimer = new EventPairTimer<int>(
            EventIdSuspendEEStart,
            EventIdRestartEEStop,
            // Suspensions/ Resumptions are always done sequentially so there is no common value to match events on. Return a constant value as the event id.
            x => 1,
            SampleEvery.OneEvent);
        
        public event Action<Events.HeapStatsEvent> HeapStats;
        public event Action<Events.PauseCompleteEvent> PauseComplete;
        public event Action<Events.CollectionStartEvent> CollectionStart;
        public event Action<Events.CollectionCompleteEvent> CollectionComplete;
        public event Action<Events.AllocationTickEvent> AllocationTick;

        public string EventSourceName => DotNetRuntimeEventSource.Name;
        public EventKeywords Keywords => (EventKeywords) DotNetRuntimeEventSource.Keywords.GC;

        public void ProcessEvent(EventWrittenEventArgs e)
        {
            if (e.EventId == EventIdAllocTick)
            {
                AllocationTick?.Invoke(Events.AllocationTickEvent.ParseFrom(e));
                return;
            }

            if (e.EventId == EventIdHeapStats)
            {
                HeapStats?.Invoke(Events.HeapStatsEvent.ParseFrom(e));
                return;
            }

            // flags representing the "Garbage Collection" + "Preparation for garbage collection" pause reasons
            const uint suspendGcReasons = 0x1 | 0x6;

            if (e.EventId == EventIdSuspendEEStart && ((uint) e.Payload[0] & suspendGcReasons) == 0)
            {
                // Execution engine is pausing for a reason other than GC, discard event.
                return;
            }

            if (_gcPauseEventTimer.TryGetDuration(e, out var pauseDuration) == DurationResult.FinalWithDuration)
            {
                PauseComplete?.Invoke(Events.PauseCompleteEvent.GetFrom(pauseDuration));
                return;
            }

            if (e.EventId == EventIdGcStart)
            {
                CollectionStart?.Invoke(Events.CollectionStartEvent.ParseFrom(e));
            }

            if (_gcEventTimer.TryGetDuration(e, out var gcDuration, out var gcData) == DurationResult.FinalWithDuration)
            {
                CollectionComplete?.Invoke(Events.CollectionCompleteEvent.GetFrom(gcData.Generation, gcData.Type, gcDuration));
            }
        }
        
        private struct GcData
        {
            public GcData(uint generation, DotNetRuntimeEventSource.GCType type)
            {
                Generation = generation;
                Type = type;
            }

            public uint Generation { get; }
            public DotNetRuntimeEventSource.GCType Type { get; }
        }

        public static class Events
        {
            public interface Info : IInfoEvents
            {
                event Action<HeapStatsEvent> HeapStats;
                event Action<PauseCompleteEvent> PauseComplete;
                event Action<CollectionStartEvent> CollectionStart;
                event Action<CollectionCompleteEvent> CollectionComplete;
            }

            public interface Verbose : IVerboseEvents
            {
                event Action<AllocationTickEvent> AllocationTick;
            }

            public class HeapStatsEvent
            {
                private static readonly HeapStatsEvent Instance = new();

                private HeapStatsEvent()
                {
                }

                public static HeapStatsEvent ParseFrom(EventWrittenEventArgs e)
                {
                    Instance.Gen0SizeBytes = ((ulong) e.Payload[0]);
                    Instance.Gen1SizeBytes = ((ulong) e.Payload[2]);
                    Instance.Gen2SizeBytes = ((ulong) e.Payload[4]);
                    Instance.LohSizeBytes = ((ulong) e.Payload[6]);
                    Instance.FinalizationQueueLength = ((ulong) e.Payload[9]);
                    Instance.NumPinnedObjects = ((uint) e.Payload[10]);

                    return Instance;
                }

                public ulong FinalizationQueueLength { get; private set; }

                public ulong LohSizeBytes { get; private set; }

                public ulong Gen2SizeBytes { get; private set; }

                public ulong Gen1SizeBytes { get; private set; }

                public uint NumPinnedObjects { get; private set; }

                public ulong Gen0SizeBytes { get; private set; }
            }

            public class PauseCompleteEvent
            {
                private static readonly PauseCompleteEvent Instance = new PauseCompleteEvent();

                private PauseCompleteEvent()
                {
                }

                public static PauseCompleteEvent GetFrom(TimeSpan pauseDuration)
                {
                    Instance.PauseDuration = pauseDuration;
                    return Instance;
                }

                public TimeSpan PauseDuration { get; private set; }
            }

            public class AllocationTickEvent
            {
                private static readonly AllocationTickEvent Instance = new();

                private AllocationTickEvent()
                {
                }

                public static AllocationTickEvent ParseFrom(EventWrittenEventArgs e)
                {
                    const uint lohHeapFlag = 0x1;
                    Instance.IsLargeObjectHeap = ((uint) e.Payload[1] & lohHeapFlag) == lohHeapFlag;
                    Instance.AllocatedBytes = (uint) e.Payload[0];

                    return Instance;
                }

                public uint AllocatedBytes { get; private set; }
                public bool IsLargeObjectHeap { get; private set; }
            }

            public class CollectionStartEvent
            {
                private static readonly CollectionStartEvent Instance = new();

                private CollectionStartEvent()
                {
                }

                public static CollectionStartEvent ParseFrom(EventWrittenEventArgs e)
                {
                    Instance.Count = (uint)e.Payload[0];
                    Instance.Generation = (uint) e.Payload[1];
                    Instance.Reason = (DotNetRuntimeEventSource.GCReason) e.Payload[2];
                    return Instance;
                }

                public uint Generation { get; private set; }
                public uint Count { get; private set; }
                public DotNetRuntimeEventSource.GCReason Reason { get; private set; }
            }

            public class CollectionCompleteEvent
            {
                private static readonly CollectionCompleteEvent Instance = new();

                private CollectionCompleteEvent()
                {
                }

                public static CollectionCompleteEvent GetFrom(uint generation, DotNetRuntimeEventSource.GCType type, TimeSpan duration)
                {
                    Instance.Generation = generation;
                    Instance.Type = type;
                    Instance.Duration = duration;

                    return Instance;
                }

                public TimeSpan Duration { get; private set; }
                public DotNetRuntimeEventSource.GCType Type { get; private set; }
                public uint Generation { get; private set; }
            }
        }
    }
}
