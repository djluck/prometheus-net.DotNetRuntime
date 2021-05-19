using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Prometheus.DotNetRuntime.EventListening;
using Prometheus.DotNetRuntime.EventListening.Parsers;
using Prometheus.DotNetRuntime.Metrics;
using Prometheus.DotNetRuntime.Metrics.Producers;
using Prometheus.DotNetRuntime.Metrics.Producers.Util;

namespace Prometheus.DotNetRuntime
{
    /// <summary>
    /// Configures what .NET core runtime metrics will be collected. 
    /// </summary>
    public static class DotNetRuntimeStatsBuilder
    {
        /// <summary>
        /// Includes all .NET runtime metrics that can be collected at the <see cref="CaptureLevel.Counters"/> capture level,
        /// ensuring minimal impact on performance. Call <see cref="Builder.StartCollecting()"/> to begin collecting metrics.
        /// </summary>
        /// <returns></returns>
        public static Builder Default()
        {
            return Customize()
                .WithContentionStats()
                .WithThreadPoolStats()
                .WithGcStats()
                .WithKestrelStats()
                .WithJitStats()
                .WithExceptionStats();
        }

        /// <summary>
        /// Allows you to customize the types of metrics collected. 
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// Include specific .NET runtime metrics by calling the WithXXX() methods and then call <see cref="Builder.StartCollecting()"/>
        /// </remarks>
        public static Builder Customize()
        {
            return new Builder();
        }

        public class Builder
        {
            private readonly DotNetRuntimeStatsCollector.Options _options = new();
            internal HashSet<ListenerRegistration> ListenerRegistrations { get; } = new();
            private readonly IServiceCollection _services = new ServiceCollection();

            public Builder()
            {
                // For now, we include runtime events by default. May make this customizable in the future.
                ListenerRegistrations.Add(ListenerRegistration.Create(CaptureLevel.Counters, sp => new RuntimeEventParser() { RefreshIntervalSeconds = 1}));

                // TODO what if we want to support extensibility? e.g. add your own custom metric generator
            }

            /// <summary>
            /// Finishes configuration and starts collecting .NET runtime metrics. Returns a <see cref="IDisposable"/> that
            /// can be disposed of to stop metric collection. 
            /// </summary>
            /// <returns></returns>
            public IDisposable StartCollecting()
            {
                return StartCollecting(Prometheus.Metrics.DefaultRegistry);
            }

            /// <summary>
            /// Finishes configuration and starts collecting .NET runtime metrics. Returns a <see cref="IDisposable"/> that
            /// can be disposed of to stop metric collection. 
            /// </summary>
            /// <param name="registry">Registry where metrics will be collected</param>
            /// <returns></returns>
            public IDisposable StartCollecting(CollectorRegistry registry)
            {
                var serviceProvider = BuildServiceProvider();
                var runtimeStatsCollector = new DotNetRuntimeStatsCollector(serviceProvider, registry, _options);
                return runtimeStatsCollector;
            }

            /// <summary>
            /// Include metrics around the size of the worker and IO thread pools and reasons
            /// for worker thread pool changes.
            /// </summary>
            public Builder WithThreadPoolStats(CaptureLevel level = CaptureLevel.Counters, ThreadPoolMetricsProducer.Options options = null)
            {
                try
                {
                    if (level != CaptureLevel.Counters)
                        ListenerRegistrations.AddOrReplace(ListenerRegistration.Create(level, sp => new ThreadPoolEventParser()));
                }
                catch (UnsupportedEventParserLevelException ex)
                {
                    throw UnsupportedCaptureLevelException.CreateWithCounterSupport(ex);
                }

                _services.TryAddSingletonEnumerable<IMetricProducer, ThreadPoolMetricsProducer>();
                _services.AddSingleton(options ?? new ThreadPoolMetricsProducer.Options());

                return this;
            }

            /// <summary>
            /// Include metrics around the kestrel stats
            /// </summary>
            /// <param name="level"></param>
            /// <param name="sampleRate">
            /// The sampling rate for contention events (defaults to 100%). A lower sampling rate reduces memory use
            /// but reduces the accuracy of metrics produced (as a percentage of events are discarded).
            /// </param>
            public Builder WithKestrelStats(CaptureLevel level = CaptureLevel.Counters, SampleEvery sampleRate = SampleEvery.TwoEvents)
            {
                try
                {
                    if (level != CaptureLevel.Counters)
                        ListenerRegistrations.AddOrReplace(ListenerRegistration.Create(CaptureLevel.Informational, sp => new KestrelEventParser(sampleRate)));
                }
                catch (UnsupportedEventParserLevelException ex)
                {
                    throw UnsupportedCaptureLevelException.CreateWithCounterSupport(ex);
                }

                _services.TryAddSingletonEnumerable<IMetricProducer, KestrelMetricsProducer>();

                return this;
            }

            /// <summary>
            /// Include metrics around volume of locks contended.
            /// </summary>
            /// <param name="level"></param>
            /// <param name="sampleRate">
            /// The sampling rate for contention events (defaults to 100%). A lower sampling rate reduces memory use
            /// but reduces the accuracy of metrics produced (as a percentage of events are discarded).
            /// </param>
            public Builder WithContentionStats(CaptureLevel level = CaptureLevel.Counters, SampleEvery sampleRate = SampleEvery.TwoEvents)
            {
                try
                {
                    if (level != CaptureLevel.Counters)
                        ListenerRegistrations.AddOrReplace(ListenerRegistration.Create(CaptureLevel.Informational, sp => new ContentionEventParser(sampleRate)));
                }
                catch (UnsupportedEventParserLevelException ex)
                {
                    throw new UnsupportedCaptureLevelException(ex);
                }

                _services.TryAddSingletonEnumerable<IMetricProducer, ContentionMetricsProducer>();

                return this;
            }

            /// <summary>
            /// Include metrics summarizing the volume of methods being compiled
            /// by the Just-In-Time compiler.
            /// </summary>
            /// <param name="captureLevel"></param>
            /// <param name="sampleRate">
            /// The sampling rate for JIT events. A lower sampling rate reduces memory use
            /// but reduces the accuracy of metrics produced (as a percentage of events are discarded).
            /// If your application achieves a high level of throughput (thousands of work items scheduled per second on
            /// the thread pool), it's recommend to reduce the sampling rate even further.
            /// </param>
            public Builder WithJitStats(CaptureLevel captureLevel = CaptureLevel.Counters, SampleEvery sampleRate = SampleEvery.TenEvents)
            {
                if (captureLevel != CaptureLevel.Counters)
                    ListenerRegistrations.AddOrReplace(ListenerRegistration.Create(CaptureLevel.Verbose, sp => new JitEventParser(sampleRate)));
                
                _services.TryAddSingletonEnumerable<IMetricProducer, JitMetricsProducer>();

                return this;
            }

            /// <summary>
            /// Include metrics recording the frequency and duration of garbage collections/ pauses, heap sizes and
            /// volume of allocations.
            /// </summary>
            /// <param name="atLevel"></param>
            /// <param name="histogramBuckets">Buckets for the GC collection and pause histograms</param>
            public Builder WithGcStats(CaptureLevel atLevel = CaptureLevel.Counters, double[] histogramBuckets = null)
            {
                try
                {
                    if (atLevel != CaptureLevel.Counters)
                        ListenerRegistrations.AddOrReplace(ListenerRegistration.Create(atLevel, sp => new GcEventParser()));
                }
                catch (UnsupportedEventParserLevelException ex)
                {
                    throw new UnsupportedCaptureLevelException(ex);
                }

                _services.TryAddSingletonEnumerable<IMetricProducer, GcMetricsProducer>();

                var opts = new GcMetricsProducer.Options();
                opts.HistogramBuckets ??= histogramBuckets;
                
                _services.AddSingleton(opts);

                return this;
            }

            /// <summary>
            /// Include metrics that measure the number of exceptions thrown.
            /// </summary>
            public Builder WithExceptionStats(CaptureLevel captureLevel = CaptureLevel.Counters)
            {
                try
                {
                    if (captureLevel != CaptureLevel.Counters)
                        ListenerRegistrations.AddOrReplace(ListenerRegistration.Create(captureLevel, sp => new ExceptionEventParser()));
                }
                catch (UnsupportedEventParserLevelException ex)
                {
                    throw new UnsupportedCaptureLevelException(ex);
                }

                _services.TryAddSingletonEnumerable<IMetricProducer, ExceptionMetricsProducer>();
                return this;
            }

            /// <summary>
            /// Specifies a function to call when an exception occurs within the .NET stats collectors.
            /// Only one error handler may be specified.
            /// </summary>
            /// <param name="handler"></param>
            /// <returns></returns>
            public Builder WithErrorHandler(Action<Exception> handler)
            {
                if (handler == null)
                    throw new ArgumentNullException(nameof(handler));
                
                _options.ErrorHandler = handler;
                return this;
            }

#if NET5_0
            /// <summary>
            /// Specifies a custom interval to recycle collectors. Defaults to 1 day.
            /// </summary>
            /// <remarks>
            /// The event collector mechanism in .NET core can in some circumstances degrade performance over time (gradual increased CPU consumption over many hours/ days).
            /// Recycling the event collectors is a workaround, preventing CPU exhaustion (see https://github.com/dotnet/runtime/issues/43985#issuecomment-793187345 for more info).
            /// During a recycle, existing metrics will not disappear/ reset but will not be updated for a short period (should be at most a couple of seconds). 
            /// </remarks>
            /// <param name="interval"></param>
            /// <returns></returns>
            public Builder RecycleCollectorsEvery(TimeSpan interval)
            {
#if DEBUG
                // In debug mode, allow more aggressive recycling times to verify recycling works correctly
                var min = TimeSpan.FromSeconds(10);
#else
                var min = TimeSpan.FromMinutes(10);
#endif
                if (interval < min)
                    throw new ArgumentOutOfRangeException(nameof(interval), $"Interval must be greater than {min}. If collectors are recycled too frequently, metrics cannot be collected accurately.");
                
                _options.RecycleListenersEvery = interval;
                return this;
            }
#endif

            /// <summary>
            /// Include additional debugging metrics. Should NOT be used in production unless debugging
            /// perf issues.
            /// </summary>
            /// <remarks>
            /// Enabling debugging will emit two metrics:
            /// 1. dotnet_debug_events_total - tracks the volume of events being processed by each stats collectorC
            /// 2. dotnet_debug_cpu_seconds_total - tracks (roughly) the amount of CPU consumed by each stats collector.  
            /// </remarks>
            /// <param name="generateDebugMetrics"></param>
            /// <returns></returns>
            public Builder WithDebuggingMetrics(bool generateDebugMetrics)
            {
                _options.EnabledDebuggingMetrics = generateDebugMetrics;
                return this;
            }
            
            private ServiceProvider BuildServiceProvider()
            {
                RegisterDefaultConsumers(_services);

                // Add the set of event listeners configured..
                _services.AddSingleton<ISet<ListenerRegistration>, HashSet<ListenerRegistration>>(_ => ListenerRegistrations);
                
                // ..and register the instance of each listener
                foreach (var r in ListenerRegistrations)
                    r.RegisterServices(_services);

                return _services.BuildServiceProvider();
            }

            internal static void RegisterDefaultConsumers(IServiceCollection services)
            {
                var interfaceType = typeof(Consumes<>);
                var concreteType = typeof(EventConsumer<>);
                
                var eventTypes = EventParserTypes.GetEventParsers()
                    .SelectMany(EventParserTypes.GetEventInterfaces);

                foreach (var t in eventTypes)
                {
                    services.AddSingleton(interfaceType.MakeGenericType(t), concreteType.MakeGenericType(t));
                }
            }
        }
    }
}