using System.Collections.Generic;
using System.Collections.Immutable;
using Prometheus.Advanced;
using Prometheus.DotNetRuntime.StatsCollectors;
using Prometheus.DotNetRuntime.StatsCollectors.Util;

namespace Prometheus.DotNetRuntime
{
    public static class DotNetRuntimeStatsBuilder
    {
        public static IOnDemandCollector Default()
        {
            // TODO expand
            return Customize()
                .WithContentionStats()
                .WithJitStats()
                .WithThreadPoolWorkStats()
                .WithGcStats()
                .Create();
        }

        public static Builder Customize()
        {
            return new Builder();
        }

        public class Builder
        {
            internal HashSet<IEventSourceStatsCollector> StatsCollectors { get; } = new HashSet<IEventSourceStatsCollector>(new TypeEquality<IEventSourceStatsCollector>());
            
            public IOnDemandCollector Create()
            {
                return new DotNetRuntimeStatsCollector(StatsCollectors.ToImmutableHashSet());
            }

            public Builder WithThreadPoolWorkStats(double[] histogramBuckets = null)
            {
                StatsCollectors.Add(new ThreadPoolWorkStatsCollector(histogramBuckets ?? Constants.DefaultHistogramBuckets));
                return this;
            }
            
            public Builder WithContentionStats()
            {
                StatsCollectors.Add(new ContentionStatsCollector());
                return this;
            }
            
            public Builder WithJitStats()
            {
                StatsCollectors.Add(new JitStatsCollector());
                return this;
            }
            
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