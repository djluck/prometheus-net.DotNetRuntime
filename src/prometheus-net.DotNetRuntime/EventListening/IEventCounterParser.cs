using Prometheus.DotNetRuntime.EventListening;

namespace Prometheus.DotNetRuntime.EventListening
{
    /// <summary>
    /// An <see cref="IEventParser{TEvents}"/> that turns untyped counter values into strongly-typed counter events.
    /// </summary>
    /// <typeparam name="TCounters"></typeparam>
    public interface IEventCounterParser<TCounters> : IEventParser<TCounters>, IEventCounterListener
        where TCounters : ICounterEvents
    {
    }
}