using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Prometheus.Advanced;
using Prometheus.DotNetRuntime.StatsCollectors;

namespace Prometheus.DotNetRuntime
{
    internal sealed class DotNetRuntimeStatsCollector : IOnDemandCollector
    {
        private DotNetEventListener[] _eventListeners;
        private readonly ImmutableHashSet<IEventSourceStatsCollector> _statsCollectors;

        internal DotNetRuntimeStatsCollector(ImmutableHashSet<IEventSourceStatsCollector> statsCollectors)
        {
            _statsCollectors = statsCollectors;
        }

        public void RegisterMetrics(ICollectorRegistry registry)
        {
            foreach (var sc in _statsCollectors)
            {
                sc.RegisterMetrics(registry);
            }
            
            // Metrics have been registered, start the event listeners
            _eventListeners = _statsCollectors
                .Select(sc => new DotNetEventListener(sc))
                .ToArray();
        }

        public void UpdateMetrics()
        {
            foreach (var sc in _statsCollectors)
            {
                sc.UpdateMetrics();
            }
        }
    }
}