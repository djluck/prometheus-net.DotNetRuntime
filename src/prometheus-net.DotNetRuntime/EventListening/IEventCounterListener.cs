namespace Prometheus.DotNetRuntime.EventListening
{
    /// <summary>
    /// An <see cref="IEventListener"/> that listens for event counters.
    /// </summary>
    public interface IEventCounterListener : IEventListener
    {
        public int RefreshIntervalSeconds { get; set; }
    }
}