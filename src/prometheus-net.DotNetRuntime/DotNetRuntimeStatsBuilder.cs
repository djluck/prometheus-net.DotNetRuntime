using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Tracing;
#if PROMV2
using Prometheus.Advanced;
#endif
using Prometheus.DotNetRuntime.StatsCollectors;
using Prometheus.DotNetRuntime.StatsCollectors.Util;

namespace Prometheus.DotNetRuntime
{
    /// <summary>
    /// Configures what .NET core runtime metrics will be collected. 
    /// </summary>
    public static class DotNetRuntimeStatsBuilder
    {
        /// <summary>
        /// Includes all available .NET runtime metrics by default. Call <see cref="Builder.StartCollecting"/>
        /// to begin collecting metrics.
        /// </summary>
        /// <returns></returns>
        public static Builder Default()
        {
            return Customize()
                .WithContentionStats()
                .WithJitStats()
                .WithThreadPoolSchedulingStats()
                .WithThreadPoolStats()
                .WithGcStats();
        }

        /// <summary>
        /// Allows you to customize the types of metrics collected. 
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// Include specific .NET runtime metrics by calling the WithXXX() methods and then call <see cref="Builder.StartCollecting"/>
        /// </remarks>
        public static Builder Customize()
        {
            return new Builder();
        }

        public class Builder
        {
            private Action<Exception> _errorHandler;
            internal HashSet<IEventSourceStatsCollector> StatsCollectors { get; } = new HashSet<IEventSourceStatsCollector>(new TypeEquality<IEventSourceStatsCollector>());
            
            /// <summary>
            /// Finishes configuration and starts collecting .NET runtime metrics. Returns a <see cref="IDisposable"/> that
            /// can be disposed of to stop metric collection. 
            /// </summary>
            /// <returns></returns>
            public IDisposable StartCollecting()
            {
                if (DotNetRuntimeStatsCollector.Instance != null)
                {
                    throw new InvalidOperationException(".NET runtime metrics are already being collected. Dispose() of your previous collector before calling this method again.");
                }
                
                var runtimeStatsCollector = new DotNetRuntimeStatsCollector(StatsCollectors.ToImmutableHashSet(), _errorHandler);
#if PROMV2
                DefaultCollectorRegistry.Instance.RegisterOnDemandCollectors(runtimeStatsCollector);
#elif PROMV3
                runtimeStatsCollector.RegisterMetrics(Metrics.DefaultRegistry);
                Metrics.DefaultRegistry.AddBeforeCollectCallback(runtimeStatsCollector.UpdateMetrics);
#endif
                return runtimeStatsCollector;
            }

            /// <summary>
            /// Include metrics around the volume of work scheduled on the worker thread pool
            /// and the scheduling delays.
            /// </summary>
            /// <param name="histogramBuckets">Buckets for the scheduling delay histogram</param>
            /// <param name="sampleRate">
            /// The sampling rate for thread pool scheduling events. A lower sampling rate reduces memory use
            /// but reduces the accuracy of metrics produced (as a percentage of events are discarded).
            /// If your application achieves a high level of throughput (thousands of work items scheduled per second on
            /// the thread pool), it's recommend to reduce the sampling rate even further.
            /// </param>
            public Builder WithThreadPoolSchedulingStats(double[] histogramBuckets = null, SampleEvery sampleRate = SampleEvery.TenEvents)
            {
                StatsCollectors.Add(new ThreadPoolSchedulingStatsCollector(histogramBuckets ?? Constants.DefaultHistogramBuckets, sampleRate));
                return this;
            }

            /// <summary>
            /// Include metrics around the size of the worker and IO thread pools and reasons
            /// for worker thread pool changes.
            /// </summary>
            public Builder WithThreadPoolStats()
            {
                StatsCollectors.Add(new ThreadPoolStatsCollector());
                return this;
            }
            
            /// <summary>
            /// Include metrics around volume of locks contended.
            /// </summary>
            /// <param name="sampleRate">
            /// The sampling rate for contention events (defaults to 100%). A lower sampling rate reduces memory use
            /// but reduces the accuracy of metrics produced (as a percentage of events are discarded).
            /// </param>
            public Builder WithContentionStats(SampleEvery sampleRate = SampleEvery.TwoEvents)
            {
                StatsCollectors.Add(new ContentionStatsCollector(sampleRate));
                return this;
            }
            
            /// <summary>
            /// Include metrics summarizing the volume of methods being compiled
            /// by the Just-In-Time compiler.
            /// </summary>
            /// <param name="sampleRate">
            /// The sampling rate for JIT events. A lower sampling rate reduces memory use
            /// but reduces the accuracy of metrics produced (as a percentage of events are discarded).
            /// If your application achieves a high level of throughput (thousands of work items scheduled per second on
            /// the thread pool), it's recommend to reduce the sampling rate even further.
            /// </param>
            public Builder WithJitStats(SampleEvery sampleRate = SampleEvery.TenEvents)
            {
                StatsCollectors.Add(new JitStatsCollector(sampleRate));
                return this;
            }
            
            /// <summary>
            /// Include metrics recording the frequency and duration of garbage collections/ pauses, heap sizes and
            /// volume of allocations.
            /// </summary>
            /// <param name="histogramBuckets">Buckets for the GC collection and pause histograms</param>
            public Builder WithGcStats(double[] histogramBuckets = null)
            {
                StatsCollectors.Add(new GcStatsCollector(histogramBuckets ?? Constants.DefaultHistogramBuckets));
                return this;
            }

            public Builder WithCustomCollector(IEventSourceStatsCollector statsCollector)
            {
                StatsCollectors.Add(statsCollector);
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
                _errorHandler = handler;
                return this;
            }
            
            internal class TypeEquality<T> : IEqualityComparer<T>
            {
                public bool Equals(T x, T y)
                {
                    return x.GetType() == y.GetType();
                }

                public int GetHashCode(T obj)
                {
                    return obj.GetType().GetHashCode();
                }
            }
        }
    }
}