using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

#if PROMV2
using Prometheus.Advanced;
using Prometheus.Advanced.DataContracts;
#endif

#if PROMV3
using Fasterflect;
#endif


namespace Prometheus.DotNetRuntime
{
    internal static class Extensions
    {
        private static readonly Type LabelType = typeof(Metrics).Assembly.GetType("Prometheus.Labels", true);
        private static readonly PropertyInfo ThreadSafeDoubleValue = typeof(Metrics).Assembly.GetType("Prometheus.ThreadSafeDouble", true).GetProperty("Value");
        private static readonly PropertyInfo ThreadSafeLongValue = typeof(Metrics).Assembly.GetType("Prometheus.ThreadSafeLong", true).GetProperty("Value");

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
        /*
         * As of Pometheus V3, the ability to inspect the values of counters + histograms has been greatly curtailed.
         * Until a decision is made in https://github.com/prometheus-net/prometheus-net/issues/136, I'll use reflection
         * to gather these values.
         * While reflection isn't fantastic for performance-focused software, these methods are mostly used to support the test
         * suite or are only called when prometheus scrapes values (i.e. not frequently).
         */
        
        /// <summary>
        /// Collects all values of a counter recorded across both unlabeled and labeled metrics.
        /// </summary>
        internal static IEnumerable<double> CollectAllValues(this Counter counter, bool excludeUnlabeled = false)
        {
            return CollectAllMetrics(counter, c => c.Value, excludeUnlabeled);
        }
        
        /// <summary>
        /// Collects all sum values of a histogram recorded across both unlabeled and labeled metrics.
        /// </summary>
        internal static IEnumerable<double> CollectAllSumValues(this Histogram histogram, bool excludeUnlabeled = false)
        {
            return CollectAllMetrics(histogram, c =>
            {
                var sumFieldVal = c.GetFieldValue("_sum");
                // for some reason, Fasterflect falls down here. Fall back to standard reflection.
                return (double) ThreadSafeDoubleValue.GetValue(sumFieldVal);
            }, excludeUnlabeled);
        }
        
        /// <summary>
        /// Collects all count values of a histogram recorded across both unlabeled and labeled metrics.
        /// </summary>
        internal static IEnumerable<ulong> CollectAllCountValues(this Histogram histogram)
        {
            return CollectAllMetrics(histogram, c =>
            {
                var bucketCountField = (IEnumerable)c.GetFieldValue("_bucketCounts");
                // for some reason, Fasterflect falls down here. Fall back to standard reflection.
                return (ulong)bucketCountField.Cast<object>().Sum(x => (long) ThreadSafeLongValue.GetValue(x));
            });
        }
        
        private static IEnumerable<TResult> CollectAllMetrics<TChild, TResult>(Collector<TChild> collector, Func<TChild, TResult> getValue, bool excludeUnlabeled = false) 
            where TChild : ChildBase
        {
            var labels = GetLabelValues(collector);
            if (!excludeUnlabeled)
                yield return getValue((TChild)collector.GetPropertyValue("Unlabelled"));
            
            foreach (var l in labels)
            {
                yield return getValue(collector.Labels(l));
            }
        }

        private static IEnumerable<string[]> GetLabelValues<TChild>(Collector<TChild> collector) 
            where TChild : ChildBase
        {
            var labelValues = collector.CallMethod("GetAllLabels", Flags.NonPublic | Flags.Instance) as object[];
            return labelValues
                .Select(x => x.GetFieldValue("Values") as string[])
                // empty case is contained in the label values, need to exclude it
                .Where(x => x.Length != 0);
        }
#endif
    }
}