using System.Diagnostics.Tracing;

namespace Prometheus.DotNetRuntime
{
    /// <summary>
    /// Specifies the fidelity of events captured.
    /// </summary>
    /// <remarks>
    /// In order to produce metrics this library collects events from the .NET runtime. The level chosen impacts both the performance of
    /// your application (the more detailed events .NET produces the more CPU it consumes to produce them) and the level of detail present in the metrics
    /// produced by this library (the more detailed events prometheus-net.DotNetRuntime captures, the more analysis it can perform). 
    /// </remarks>
    public enum CaptureLevel
    {
        /// <summary>
        /// Collect event counters only- limited metrics will be available.
        /// </summary>
        Counters = EventLevel.LogAlways,
        Errors = EventLevel.Error,
        Informational = EventLevel.Informational,
        /// <summary>
        /// Collects events at level Verbose and all other levels- produces the highest level of metric detail.
        /// </summary>
        Verbose = EventLevel.Verbose,
    }
}