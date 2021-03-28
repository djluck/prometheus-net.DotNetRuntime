namespace Prometheus.DotNetRuntime.EventListening
{
    public readonly struct MeanCounterValue
    {
        public MeanCounterValue(int count, double mean)
        {
            Count = count;
            Mean = mean;
        }

        public int Count { get; }
        public double Mean { get; }
        public double Total => Count * Mean;
    }
    
    public readonly struct IncrementingCounterValue
    {
        public IncrementingCounterValue(double value)
        {
            IncrementedBy = value;
        }

        public double IncrementedBy { get; }
    }
}