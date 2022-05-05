using System;
using System.Diagnostics.Tracing;

namespace Prometheus.DotNetRuntime.EventListening.Parsers
{
    public class NameResolutionEventParser : EventCounterParserBase<NameResolutionEventParser>, NameResolutionEventParser.Events.CountersV5_0
    {

#pragma warning disable CS0067
        [CounterName("dns-lookups-requested")]
        public event Action<MeanCounterValue> DnsLookupsRequested;

        [CounterName("dns-lookups-duration")]
        public event Action<MeanCounterValue> DnsLookupsDuration;
#pragma warning restore CS0067

        public override string EventSourceName => "System.Net.NameResolution";

        public static class Events
        {
            public interface CountersV5_0 : ICounterEvents
            {
                event Action<MeanCounterValue> DnsLookupsRequested;
                event Action<MeanCounterValue> DnsLookupsDuration;
            }
        }
    }
}
