using System.Collections.Generic;
using System.Collections.Immutable;
using Prometheus.Advanced;
using Prometheus.DotNetRuntime.StatsCollectors;
using Prometheus.DotNetRuntime.StatsCollectors.Util;

namespace Prometheus.DotNetRuntime
{
    /// <summary>
    /// Configures a new <see cref="IOnDemandCollector"/> that will collect .NET core runtime statistics.
    /// </summary>
    public static class DotNetRuntimeStatsBuilder
    {
        /// <summary>
        /// Returns a <see cref="IOnDemandCollector"/> that will capture all available .NET runtime metrics
        /// by default. 
        /// </summary>
        /// <returns></returns>
        public static IOnDemandCollector Default()
        {
            return Customize()
                .WithContentionStats()
                .WithJitStats()
                .WithThreadPoolSchedulingStats()
                .WithThreadPoolStats()
                .WithGcStats()
                .Create();
        }

        /// <summary>
        /// Allows you to customize the types of metrics collected. 
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// Include specific .NET runtime metrics by calling the WithXXX() methods and then call Create().
        /// </remarks>
        public static Builder Customize()
        {
            return new Builder();
        }

        public class Builder
        {
            internal HashSet<IEventSourceStatsCollector> StatsCollectors { get; } = new HashSet<IEventSourceStatsCollector>(new TypeEquality<IEventSourceStatsCollector>());
            
            /// <summary>
            /// Finishes configuration and returns a <see cref="IOnDemandCollector"/> that will
            /// collect the specified .NET metrics.  
            /// </summary>
            /// <returns></returns>
            public IOnDemandCollector Create()
            {
                return new DotNetRuntimeStatsCollector(StatsCollectors.ToImmutableHashSet());
            }

            /// <summary>
            /// Include metrics around the volume of work scheduled on the worker thread pool
            /// and the scheduling delays.
            /// </summary>
            /// <param name="histogramBuckets">Buckets for the scheduling delay histogram</param>
            public Builder WithThreadPoolSchedulingStats(double[] histogramBuckets = null)
            {
                StatsCollectors.Add(new ThreadPoolSchedulingStatsCollector(histogramBuckets ?? Constants.DefaultHistogramBuckets));
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
            public Builder WithContentionStats()
            {
                StatsCollectors.Add(new ContentionStatsCollector());
                return this;
            }
            
            /// <summary>
            /// Include metrics summarizing the volume of methods being compiled
            /// by the Just-In-Time compiler.
            /// </summary>
            public Builder WithJitStats()
            {
                StatsCollectors.Add(new JitStatsCollector());
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

            /// <summary>
            /// 
            /// </summary>
            /// <param name="statsCollector"></param>
            public Builder WithCustomCollector(IEventSourceStatsCollector statsCollector)
            {
                StatsCollectors.Add(statsCollector);
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