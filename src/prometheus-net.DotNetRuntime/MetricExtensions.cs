using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

#if PROMV2
using Prometheus.Advanced;
using Prometheus.Advanced.DataContracts;
#endif


namespace Prometheus.DotNetRuntime
{
    /// <summary>
    /// Provides helper functions for accessing values of metrics.
    /// </summary>
    /// <remarks>
    /// The API around accessing metric values dramatically altered in V3. This wrapper class provides support
    /// for both v2.* + v3.* of prometheus-net.
    /// </remarks>
    internal static class MetricExtensions
    {
#if PROMV2
        /// <summary>
        /// Collects all values of a counter recorded across both unlabeled and labeled metrics.
        /// </summary>
        internal static IEnumerable<double> CollectAllValues(this Counter counter, bool excludeUnlabeled = false)
        {
            return CollectAllMetrics(counter, excludeUnlabeled).Select(x => x.counter.value);
        }
        
        /// <summary>
        /// Collects all sum values of a histogram recorded across both unlabeled and labeled metrics.
        /// </summary>
        internal static IEnumerable<double> CollectAllSumValues(this Histogram histogram, bool excludeUnlabeled = false)
        {
            return CollectAllMetrics(histogram, excludeUnlabeled).Select(x => x.histogram.sample_sum);
        }
        
        /// <summary>
        /// Collects all count values of a histogram recorded across both unlabeled and labeled metrics.
        /// </summary>
        internal static IEnumerable<ulong> CollectAllCountValues(this Histogram histogram)
        {
            return CollectAllMetrics(histogram).Select(x => x.histogram.sample_count);
        }
        
        internal static IEnumerable<Metric> CollectAllMetrics(this ICollector collector, bool excludeUnlabeled = false)
        {
            return collector.Collect().Single().metric.Where(x => !excludeUnlabeled || x.label.Count > 0);
        }
#endif

#if PROMV3
        /// <summary>
        /// Collects all values of a counter recorded across both unlabeled and labeled metrics.
        /// </summary>
        internal static IEnumerable<double> CollectAllValues(this Counter counter, bool excludeUnlabeled = false)
        {
            IHistogram h;
            return CollectAllMetrics<Counter, Counter.Child, ICounter, double>(counter, c => c.Value, excludeUnlabeled);
        }

        /// <summary>
        /// Collects all sum values of a histogram recorded across both unlabeled and labeled metrics.
        /// </summary>
        internal static IEnumerable<double> CollectAllSumValues(this Histogram histogram, bool excludeUnlabeled = false)
        {
            return CollectAllMetrics<Histogram, Histogram.Child, IHistogram, double>(histogram,c => c.Sum, excludeUnlabeled);
        }

        /// <summary>
        /// Collects all count values of a histogram recorded across both unlabeled and labeled metrics.
        /// </summary>
        internal static IEnumerable<ulong> CollectAllCountValues(this Histogram histogram)
        {
            return CollectAllMetrics<Histogram, Histogram.Child, IHistogram, ulong>(histogram,c => (ulong)c.Count);
        }

        private static IEnumerable<TResult> CollectAllMetrics<TCollector, TChild, TInterface, TResult>(TCollector collector, Func<TInterface, TResult> getValue, bool excludeUnlabeled = false)
            where TCollector : Collector<TChild>, TInterface
            where TChild : ChildBase, TInterface
        {
            var labels = GetLabelValues(collector);
            if (!excludeUnlabeled)
                yield return getValue((TInterface) collector);

            foreach (var l in labels)
            {
                yield return getValue(collector.Labels(l));
            }
        }

        private static IEnumerable<string[]> GetLabelValues<TChild>(Collector<TChild> collector)
            where TChild : ChildBase
        {
            return collector.GetAllLabelValues();
        }
#endif
    }
}