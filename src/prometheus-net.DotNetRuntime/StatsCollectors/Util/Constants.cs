namespace Prometheus.DotNetRuntime.StatsCollectors.Util
{
    internal class Constants
    {
        /// <summary>
        /// In seconds, the buckets to use when generating histogram.
        /// </summary>
        /// <remarks>
        /// Default is: 1ms, 10ms, 50ms, 100ms, 500ms, 1 sec, 10 sec
        /// </remarks>
        internal static readonly double[] DefaultHistogramBuckets = {0.001, 0.01, 0.05, 0.1, 0.5, 1, 10};
    }
}