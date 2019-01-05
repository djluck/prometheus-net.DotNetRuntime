using System.Collections.Generic;
using System.Linq;
using Prometheus.Advanced;
using Prometheus.Advanced.DataContracts;

namespace Prometheus.DotNetRuntime
{
    internal static class Extensions
    {
        internal static List<Metric> CollectSingle(this ICollector collector)
        {
            return collector.Collect().Single().metric;
        } 
    }
}