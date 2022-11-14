using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Prometheus.DotNetRuntime.EventListening.Parsers;
using Prometheus.DotNetRuntime.Metrics.Producers;

namespace Prometheus.DotNetRuntime.Tests.IntegrationTests
{
    //[TestFixture]
    //internal class Given_Contention_Events_Are_Enabled_For_Kestrel_Stats 
    //{
    //  [Test]
    //    public void Will_measure_no_contention_on_an_uncontested_lock()
    //    {
    //        var kestrelInfo = new EventConsumer<KestrelEventParser.Events.Info>();
    //        var blah = new KestrelMetricsProducer(kestrelInfo, new EventConsumer<RuntimeEventParser.Events.Counters>());

    //        kestrelInfo.Events.ConnectionStart =>

    //        // assert
    //        Assert.That(MetricProducer.ConnectionTotal.Value, Is.EqualTo(1));
    //        Assert.That(MetricProducer.CurrentConnectionCount.Value, Is.EqualTo(0)); 
    //    }
    //}
}