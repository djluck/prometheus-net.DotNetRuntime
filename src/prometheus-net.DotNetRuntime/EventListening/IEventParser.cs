using System;
using System.Collections.Immutable;
using System.Diagnostics.Tracing;
using Prometheus.DotNetRuntime.EventListening;

namespace Prometheus.DotNetRuntime.EventListening
{
    /// <summary>
    /// A <see cref="IEventListener"/> that receives "untyped" events of <see cref="EventWrittenEventArgs"/> into strongly-typed events.
    /// </summary>
    /// <typeparam name="TEvents">
    /// Represents the set of strongly-typed events emitted by this parser. Implementors should not directly implement <see cref="IEvents"/>, rather
    /// implement inheriting interfaces such as <see cref="IInfoEvents"/>, <see cref="IWarningEvents"/>, etc. 
    /// </typeparam>
    public interface IEventParser<TEvents> : IEventListener
        where TEvents : IEvents
    {
        ImmutableHashSet<EventLevel> IEventListener.SupportedLevels => EventParserDefaults.GetSupportedLevels(this);

        private static class EventParserDefaults
        {
            private static ImmutableHashSet<EventLevel> SupportedLevels;
        
            public static ImmutableHashSet<EventLevel> GetSupportedLevels(IEventParser<TEvents> listener)
            {
                if (SupportedLevels == null)
                    SupportedLevels = EventParserTypes.GetLevelsFromParser(listener.GetType());
                
                return SupportedLevels;
            }
        }
    }
}