using System;
using System.Collections.Immutable;
using System.Diagnostics.Tracing;

namespace Prometheus.DotNetRuntime.EventListening
{
    public interface IEventListener : IDisposable
    {
        /// <summary>
        /// The unique id of the event source to receive events from.
        /// </summary>
        Guid EventSourceGuid { get; }
        
        /// <summary>
        /// The keywords to enable in the event source.
        /// </summary>
        /// <remarks>
        /// Keywords act as a "if-any-match" filter- specify multiple keywords to obtain multiple categories of events
        /// from the event source.
        /// </remarks>
        EventKeywords Keywords { get; }
        
        /// <summary>
        /// The levels of events supported.
        /// </summary>
        ImmutableHashSet<EventLevel> SupportedLevels { get; }
        
        /// <summary>
        /// Process a received event.
        /// </summary>
        /// <remarks>
        /// Implementors should listen to events and perform some kind of processing.
        /// </remarks>
        void ProcessEvent(EventWrittenEventArgs e);
        
        void IDisposable.Dispose()
        {
        }
    }
}