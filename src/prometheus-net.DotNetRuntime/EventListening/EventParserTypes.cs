using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Reflection;

namespace Prometheus.DotNetRuntime.EventListening
{
    internal static class EventParserTypes
    {
        private static readonly ImmutableHashSet<Type> InterfaceTypesToIgnore = new[]
        {
            typeof(IEvents),
            typeof(IVerboseEvents),
            typeof(IInfoEvents),
            typeof(IWarningEvents),
            typeof(IErrorEvents),
            typeof(IAlwaysEvents),
            typeof(ICriticalEvents),
            typeof(ICounterEvents),
        }.ToImmutableHashSet();
        
        internal static IEnumerable<Type> GetEventInterfaces(Type t)
        {
            return t.GetInterfaces()
                .Where(i => typeof(IEvents).IsAssignableFrom(i) && !InterfaceTypesToIgnore.Contains(i));
        }
        
        internal static IEnumerable<Type> GetEventInterfaces(Type t, EventLevel atLevelAndBelow)
        {
            return GetEventInterfaces(t)
                .Where(t => GetEventLevel(t) <= atLevelAndBelow);
        }
        
        internal static ImmutableHashSet<EventLevel> GetLevelsFromParser(Type type)
        {
            return GetEventInterfaces(type)
                .Select(GetEventLevel)
                .ToImmutableHashSet();
        }

        private static EventLevel GetEventLevel(Type t)
        {
            // Captures ICounterEvents too as it inherits from IAlwaysEvents
            if (typeof(IAlwaysEvents).IsAssignableFrom(t))
                return EventLevel.LogAlways;

            if (typeof(IVerboseEvents).IsAssignableFrom(t))
                return EventLevel.Verbose;

            if (typeof(IInfoEvents).IsAssignableFrom(t))
                return EventLevel.Informational;

            if (typeof(IWarningEvents).IsAssignableFrom(t))
                return EventLevel.Warning;

            if (typeof(IErrorEvents).IsAssignableFrom(t))
                return EventLevel.Error;

            if (typeof(ICriticalEvents).IsAssignableFrom(t))
                return EventLevel.Critical;

            throw new InvalidOperationException($"Unexpected type '{t}'");
        }

        internal static IEnumerable<Type> GetEventParsers()
        {
            return GetEventParsers(typeof(IEventListener).Assembly);
        }
        
        internal static IEnumerable<Type> GetEventParsers(Assembly fromAssembly)
        {
            return fromAssembly
                .GetTypes()
                .Where(x => x.IsClass && x.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventParser<>)));
        }
    }
}