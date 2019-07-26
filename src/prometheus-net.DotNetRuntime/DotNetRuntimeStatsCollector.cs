using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
#if PROMV2
using Prometheus.Advanced;
#endif
using Prometheus.DotNetRuntime.StatsCollectors;

namespace Prometheus.DotNetRuntime
{
    internal sealed class DotNetRuntimeStatsCollector : 
        IDisposable
#if PROMV2
        , IOnDemandCollector
#endif
    {
        private DotNetEventListener[] _eventListeners;
        private readonly ImmutableHashSet<IEventSourceStatsCollector> _statsCollectors;
        private readonly bool _enabledDebugging;
        private readonly Action<Exception> _errorHandler;

        internal DotNetRuntimeStatsCollector(ImmutableHashSet<IEventSourceStatsCollector> statsCollectors, Action<Exception> errorHandler, bool enabledDebugging)
        {
            _statsCollectors = statsCollectors;
            _enabledDebugging = enabledDebugging;
            _errorHandler = errorHandler ?? (e => { });
            Instance = this;
        }
        
        internal static DotNetRuntimeStatsCollector Instance { get; private set; }

        public void RegisterMetrics(
#if PROMV2
            ICollectorRegistry registry
#elif PROMV3
            CollectorRegistry registry
#endif
        )
        {
            
#if PROMV2
            var metrics = new MetricFactory(registry);
#elif PROMV3
            var metrics = Metrics.WithCustomRegistry(registry);
#endif
            
            foreach (var sc in _statsCollectors)
            {
                sc.RegisterMetrics(metrics);
            }
            
            // Metrics have been registered, start the event listeners
            _eventListeners = _statsCollectors
                .Select(sc => new DotNetEventListener(sc, _errorHandler, _enabledDebugging))
                .ToArray();
        }

        public void UpdateMetrics()
        {
            foreach (var sc in _statsCollectors)
            {
                try
                {
                    sc.UpdateMetrics();
                }
                catch (Exception e)
                {
                    _errorHandler(e);
                }
            }
        }

        public void Dispose()
        {
            try
            {
                if (_eventListeners == null)
                    return;

                foreach (var listener in _eventListeners)
                    listener?.Dispose();
            }
            finally
            {
                Instance = null;
            }
        }
    }
}