using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Prometheus.DotNetRuntime.EventListening;
using Prometheus.DotNetRuntime.Metrics;

namespace Prometheus.DotNetRuntime
{
    internal sealed class DotNetRuntimeStatsCollector : 
        IDisposable
    {
        private static readonly Dictionary<CollectorRegistry, DotNetRuntimeStatsCollector> Instances = new();
        
        private readonly CollectorRegistry _metricRegistry;
        private readonly Options _options;
        private readonly object _lockInstance = new ();
        private readonly CancellationTokenSource _ctSource = new();
        private readonly Task _recycleTask;
        private bool _disposed = false;
        private DotNetEventListener.GlobalOptions _listenerGlobalOpts;

        internal DotNetRuntimeStatsCollector(ServiceProvider serviceProvider, CollectorRegistry metricRegistry, Options options)
        {
            _metricRegistry = metricRegistry;
            _options = options;
            ServiceProvider = serviceProvider;
            var metrics = Prometheus.Metrics.WithCustomRegistry(_metricRegistry);
            _listenerGlobalOpts = DotNetEventListener.GlobalOptions.CreateFrom(_options, metrics);
            
            lock (_lockInstance)
            {
                if (Instances.ContainsKey(_metricRegistry))
                {
                    throw new InvalidOperationException(".NET runtime metrics are already being collected. Dispose() of your previous collector before calling this method again.");
                }

                Instances.Add(_metricRegistry, this);
            }

            RegisterMetrics(metrics);
            EventListeners = CreateEventListeners();
            if (options.RecycleListenersEvery != null)
                _recycleTask = Task.Factory.StartNew(() => RestartListeningEvery(options.RecycleListenersEvery.Value), TaskCreationOptions.LongRunning).Unwrap();
        }

        private DotNetEventListener[] CreateEventListeners()
        {
            return ServiceProvider
                .GetService<ISet<ListenerRegistration>>()
                .Select(r => new DotNetEventListener((IEventListener) ServiceProvider.GetService(r.Type), r.Level, _listenerGlobalOpts))
                .ToArray();
        }

        internal DotNetEventListener[] EventListeners { get; private set; }
        internal ServiceProvider ServiceProvider { get; }
        internal Counter EventListenerRecycles { get; private set; }

        public void RegisterMetrics(MetricFactory metrics)
        {
            foreach (var mp in ServiceProvider.GetServices<IMetricProducer>())
                mp.RegisterMetrics(metrics);

            _metricRegistry.AddBeforeCollectCallback(UpdateMetrics);

            if (_options.RecycleListenersEvery.HasValue)
                EventListenerRecycles = metrics.CreateCounter("dotnet_internal_recycle_count", "prometheus-net.DotNetRuntime internal metric. Counts the number of times the underlying event listeners have been recycled");
            
            SetupConstantMetrics(metrics);
        }

        public void UpdateMetrics()
        {
            // prometheus-net currently offers no mechanism to unregister collection callbacks added by AddBeforeCollectCallback.
            // Once disposed to avoid errors, just exit immediately.
            if (_disposed)
                return;
            
            foreach (var mp in ServiceProvider.GetServices<IMetricProducer>())
            {
                try
                {
                    mp.UpdateMetrics();
                }
                catch (Exception e)
                {
                    _options.ErrorHandler(e);
                }
            }
        }
        
        private async Task RestartListeningEvery(TimeSpan recycleEvery)
        {
            while (!_ctSource.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(recycleEvery, _ctSource.Token);
                    
                    // While it's slightly misleading to record a recycle as having taken place before completing, there is a known 
                    // race condition in https://github.com/dotnet/runtime/issues/40190 that can occur if listeners are disabled/ re-enabled in quick succession.
                    // Record this now so if this happens in the wild, people will be able to spot the issue.
                    EventListenerRecycles.Inc();

                    foreach (var el in EventListeners)
                    {
                        el.Dispose();
                    }

                    EventListeners = CreateEventListeners();
                }
                catch (OperationCanceledException) when (_ctSource.IsCancellationRequested)
                {
                    // swallow, expected on dispose
                }
                catch (Exception ex)
                {
                    _options.ErrorHandler(ex);
                }
            }
        }

        public void Dispose()
        {
            try
            {
                _ctSource.Cancel();
                _recycleTask?.Wait(TimeSpan.FromSeconds(1));

                if (EventListeners != null)
                {
                    foreach (var listener in EventListeners)
                        listener?.Dispose();
                }

                ServiceProvider.Dispose();
            }
            finally
            {
                lock (_lockInstance)
                {
                    Instances.Remove(_metricRegistry);
                }

                _disposed = true;
            }
        }
        
        private void SetupConstantMetrics(MetricFactory metrics)
        {
            // These metrics are fairly generic in name, catch any exceptions on trying to create them 
            // in case prometheus-net or another plugin has registered them.
            try
            {
                var buildInfo = metrics.CreateGauge(
                    "dotnet_build_info",
                    "Build information about prometheus-net.DotNetRuntime and the environment",
                    "version",
                    "target_framework",
                    "runtime_version",
                    "os_version",
                    "process_architecture",
                    "gc_mode"
                );

                buildInfo.Labels(
                        this.GetType().Assembly.GetName().Version.ToString(),
                        Assembly.GetEntryAssembly().GetCustomAttribute<TargetFrameworkAttribute>().FrameworkName,
                        RuntimeInformation.FrameworkDescription,
                        RuntimeInformation.OSDescription,
                        RuntimeInformation.ProcessArchitecture.ToString(),
                        GCSettings.IsServerGC ? "Server" : "Workstation"
                    )
                    .Set(1);
            }
            catch (Exception e)
            {
                _options.ErrorHandler(e);
            }

            try
            {
                var processorCount = metrics.CreateGauge("process_cpu_count", "The number of processor cores available to this process.");
                processorCount.Set(Environment.ProcessorCount);
            }
            catch (Exception e)
            {
                _options.ErrorHandler(e);
            }
        }
        
        public class Options
        {
            public Action<Exception> ErrorHandler { get; set; } = (e => { });
            public bool EnabledDebuggingMetrics { get; set; } = false;

            public TimeSpan? RecycleListenersEvery { get; set; } =
#if NET5_0
                // only default to enabled for .NET 5. .NET 6 had/ has issues where recycling collectors could lead to 
                // problems, see https://github.com/dotnet/runtime/pull/76431
                // HOWEVER, people have mentioned that recycling is still required under .NET 6.0: https://github.com/dotnet/runtime/pull/76431
                // As a compromise, we won't enable it by default but will allow people to opt-in
                TimeSpan.FromDays(1);
#else
                null;
#endif

        }
    }
}