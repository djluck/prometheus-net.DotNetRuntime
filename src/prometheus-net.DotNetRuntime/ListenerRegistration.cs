using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Tracing;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Prometheus.DotNetRuntime.EventListening;

namespace Prometheus.DotNetRuntime
{
    internal class ListenerRegistration : IEquatable<ListenerRegistration>
    {
        private ListenerRegistration(EventLevel level, Type type, Func<IServiceProvider, object> factory)
        {
            Level = level;
            Type = type;
            Factory = factory;
        }

        public static ListenerRegistration Create<T>(CaptureLevel level, Func<IServiceProvider, T> factory)
            where T : IEventListener
        {
            var supportedLevels = EventParserTypes.GetLevelsFromParser(typeof(T));
            var eventLevel = level.ToEventLevel();

            if (!supportedLevels.Contains(eventLevel))
                throw new UnsupportedEventParserLevelException(typeof(T), level, supportedLevels);


            if (!EventParserTypes.AreEventsSupportedByRuntime(typeof(T)))
                throw new UnsupportedEventParserRuntimeException(typeof(T));
                    

            return new ListenerRegistration(eventLevel, typeof(T), sp => (object)factory(sp));
        }

        internal void RegisterServices(IServiceCollection services)
        {
            services.AddSingleton(Type, Factory);
            services.AddSingleton(typeof(IEventListener), sp => sp.GetService(Type));
                    
            // Register each events interface exposed at the level specified
            foreach (var i in EventParserTypes.GetEventInterfacesForCurrentRuntime(Type, Level))
                services.AddSingleton(i, sp => sp.GetService(Type));
        }

        public EventLevel Level { get; set; }
        public Type Type { get; }
        public Func<IServiceProvider, object> Factory { get; }

        public bool Equals(ListenerRegistration other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Type, other.Type);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ListenerRegistration) obj);
        }

        public override int GetHashCode()
        {
            return (Type != null ? Type.GetHashCode() : 0);
        }
    }

    internal class UnsupportedEventParserRuntimeException : Exception
    {
        public Type Type { get; }

        public UnsupportedEventParserRuntimeException(Type type)
            : base($"{EventParserTypes.AreEventsSupportedByRuntime(type)}")
        {
            Type = type;
        }
    }

    public class UnsupportedCaptureLevelException : Exception
    {
        public UnsupportedCaptureLevelException(CaptureLevel specifiedLevel, ISet<CaptureLevel> supportedLevels)
            : base($"The level '{specifiedLevel}' is not supported- please use one of: {string.Join(", ", supportedLevels)}")
        {
            SpecifiedLevel = specifiedLevel;
            SupportedLevels = supportedLevels;
        }
        
        public UnsupportedCaptureLevelException(UnsupportedEventParserLevelException ex)
            : this (ex.SpecifiedLevel, ex.SupportedLevels.Select(x => x.ToCaptureLevel()).ToImmutableHashSet())
        {
        }

        public static UnsupportedCaptureLevelException CreateWithCounterSupport(UnsupportedEventParserLevelException ex)
        {
            return new (
                ex.SpecifiedLevel, 
                ex.SupportedLevels
                    .Select(x => x.ToCaptureLevel())
                    .ToImmutableHashSet()
                    .Add(CaptureLevel.Counters)
            );
        }
        
        public CaptureLevel SpecifiedLevel { get; }
        public ISet<CaptureLevel> SupportedLevels { get; }
    }

    public class UnsupportedEventParserLevelException : Exception
    {
        public UnsupportedEventParserLevelException(Type eventParserType, CaptureLevel specifiedLevel, ISet<EventLevel> supportedLevels)
            : base($"The event parser '{eventParserType.Name}' does not support the level '{specifiedLevel}'- please use one of: {string.Join(", ", supportedLevels)}")
        {
            EventParserType = eventParserType;
            SpecifiedLevel = specifiedLevel;
            SupportedLevels = supportedLevels;
        }
        
        public Type EventParserType { get; }
        public CaptureLevel SpecifiedLevel { get; }
        public ISet<EventLevel> SupportedLevels { get; }
    }
}