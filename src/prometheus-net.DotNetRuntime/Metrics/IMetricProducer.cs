using System;
using System.Threading;
using System.Threading.Tasks;

namespace Prometheus.DotNetRuntime.Metrics
{
    public interface IMetricProducer
    {
        /// <summary>
        /// Called when the producer is associated with a metrics registry, allowing metrics to be created via the passed <see cref="MetricFactory"/>.
        /// </summary>
        void RegisterMetrics(MetricFactory metrics);

        /// <summary>
        /// Called before each metrics collection. Any metrics managed by this producer should now be brought up to date.
        /// </summary>
        void UpdateMetrics();
    }
}