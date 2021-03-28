using System;
using System.Diagnostics.Tracing;
using Prometheus.DotNetRuntime.EventListening.EventSources;
using Prometheus.DotNetRuntime.EventListening.Parsers;
using Prometheus.DotNetRuntime.EventListening.Parsers.Util;

namespace Prometheus.DotNetRuntime.EventListening.Parsers
{
    public class JitEventParser : IEventParser<JitEventParser>, JitEventParser.Events.Verbose
    {
        private readonly SamplingRate _samplingRate;
        private const int EventIdMethodJittingStarted = 145, EventIdMethodLoadVerbose = 143;
        private readonly EventPairTimer<ulong> _eventPairTimer;

        public event Action<Events.CompilationCompleteEvent> CompilationComplete;

        public JitEventParser(SamplingRate samplingRate)
        {
            _samplingRate = samplingRate;
            _eventPairTimer = new EventPairTimer<ulong>(
                EventIdMethodJittingStarted,
                EventIdMethodLoadVerbose,
                x => (ulong)x.Payload[0],
                samplingRate
            );
        }
       
        public EventKeywords Keywords => (EventKeywords) DotNetRuntimeEventSource.Keywords.Jit;
        public Guid EventSourceGuid => DotNetRuntimeEventSource.Id;

        public void ProcessEvent(EventWrittenEventArgs e)
        {
            if (_eventPairTimer.TryGetDuration(e, out var duration) == DurationResult.FinalWithDuration)
            {
                CompilationComplete?.InvokeManyTimes(_samplingRate.SampleEvery, Events.CompilationCompleteEvent.ParseFrom(e, duration));
            }
        }

        public static class Events
        {
            public interface Verbose : IVerboseEvents
            {
                event Action<CompilationCompleteEvent> CompilationComplete;
            }
            
            public class CompilationCompleteEvent
            {
                private static readonly CompilationCompleteEvent Instance = new();
            
                private CompilationCompleteEvent() { }
            
                public TimeSpan CompilationDuration { get; private set; }
                public bool IsMethodDynamic { get; private set; }

                public static CompilationCompleteEvent ParseFrom(EventWrittenEventArgs e, TimeSpan compilationDuration)
                {
                    // dynamic methods are of special interest to us- only a certain number of JIT'd dynamic methods
                    // will be cached. Frequent use of dynamic can cause methods to be evicted from the cache and re-JIT'd
                    var methodFlags = (uint)e.Payload[5];
                    Instance.IsMethodDynamic = (methodFlags & 0x1) == 0x1;
                    Instance.CompilationDuration = compilationDuration;
                
                    return Instance;
                }
            }
        }
    }
}