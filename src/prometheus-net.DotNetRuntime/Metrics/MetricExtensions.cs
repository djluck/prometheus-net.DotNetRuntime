using System;
using System.Collections.Generic;

namespace Prometheus.DotNetRuntime.Metrics
{
    /// <summary>
    /// Provides helper functions for accessing values of metrics.
    /// </summary>
    internal static class MetricExtensions
    {
        /// <summary>
        /// Collects all values of a counter recorded across both unlabeled and labeled metrics.
        /// </summary>
        internal static IEnumerable<double> CollectAllValues(this Counter counter, bool excludeUnlabeled = false)
        {
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
    }
}