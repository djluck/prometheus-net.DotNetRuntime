namespace Prometheus.DotNetRuntime
{
    /// <summary>
    /// Determines the level of sampling stats collectors will perform. <see cref="OneEvent"/> offers the highest level
    /// of accuracy while <see cref="HundredEvents"/> offers the lowest level of precision but least amount of overhead.
    /// </summary>
    public enum SampleEvery
    {
        /// <summary>
        /// The highest level of accuracy- every event will be sampled.
        /// </summary>
        OneEvent = 1,
        TwoEvents = 2,
        FiveEvents = 5,
        TenEvents = 10,
        TwentyEvents = 20,
        FiftyEvents = 50,
        /// <summary>
        /// The lowest level of precision- only 1 in 100 events will be sampled.
        /// </summary>
        HundredEvents = 100
    }
}