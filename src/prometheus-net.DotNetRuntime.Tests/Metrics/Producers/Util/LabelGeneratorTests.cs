using NUnit.Framework;
using Prometheus.DotNetRuntime.EventListening.EventSources;
using Prometheus.DotNetRuntime.Metrics.Producers.Util;

namespace Prometheus.DotNetRuntime.Tests.Metrics.Producers.Util
{
    [TestFixture]
    public class LabelGeneratorTests
    {
        [Test]
        public void MapEnumToLabelValues_will_generate_labels_with_snake_cased_names()
        {
            var labels = LabelGenerator.MapEnumToLabelValues<DotNetRuntimeEventSource.GCReason>();
            
            Assert.That(labels[DotNetRuntimeEventSource.GCReason.AllocLarge], Is.EqualTo("alloc_large"));
            Assert.That(labels[DotNetRuntimeEventSource.GCReason.OutOfSpaceLOH], Is.EqualTo("out_of_space_loh"));
        }
    }
}