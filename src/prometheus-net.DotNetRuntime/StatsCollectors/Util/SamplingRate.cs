using System.Threading;

namespace Prometheus.DotNetRuntime.StatsCollectors.Util
{
    /// <summary>
    /// The rate at which high-frequency events are sampled
    /// </summary>
    /// <remarks>
    /// In busy .NET applications, certain events are emitted at the rate of thousands/ tens of thousands per second.
    /// To track all the start and end pairs of events for these events can consume a significant amount of memory (100+ MB).
    /// Using a sampling rate allows us to reduce the memory requirements. 
    /// </remarks>
    public sealed class SamplingRate
    {
        private long _next;
        
        public SamplingRate(SampleEvery every)
        {
            SampleEvery = (int)every;
            _next = 0L;
        }
        
        /// <summary>
        /// Out of every 100 events, how many events we should observe.
        /// </summary>
        public int SampleEvery { get; }

        /// <summary>
        /// Determines if we should sample a given event.
        /// </summary>
        /// <returns></returns>
        public bool ShouldSampleEvent()
        {
            if (SampleEvery == 1)
                return true;
            
            return (Interlocked.Increment(ref _next) % SampleEvery) == 0;
        }
        
        public static implicit operator SamplingRate(SampleEvery d) => new SamplingRate(d);
    }
}