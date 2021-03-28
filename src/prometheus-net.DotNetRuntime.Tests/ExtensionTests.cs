using System.Linq;
using NUnit.Framework;
using Prometheus.DotNetRuntime.Metrics;

namespace Prometheus.DotNetRuntime.Tests
{
    [TestFixture]
    public class ExtensionTests
    {
        [Test]
        public void CollectAllValues_Extracts_All_Labeled_And_Unlabeled_Values_From_A_Counter()
        {
            // arrange
            var counter = Prometheus.Metrics.CreateCounter("test_counter", "", "label1", "label2");
            counter.Inc(); // unlabeled
            counter.Labels("1", "2").Inc();
            counter.Labels("1", "3").Inc(2);
            
            // act
            var values = MetricExtensions.CollectAllValues(counter);
            
            // assert
            Assert.That(values.Count(), Is.EqualTo(3));
            Assert.That(values.Sum(), Is.EqualTo(4));
        }
        
        [Test]
        public void CollectAllValues_Extracts_All_Labeled_Values_From_A_Counter_When_excludeUnlabeled_Is_True()
        {
            // arrange
            var counter = Prometheus.Metrics.CreateCounter("test_counter2", "", "label1", "label2");
            counter.Inc(); // unlabeled
            counter.Labels("1", "2").Inc();
            counter.Labels("1", "3").Inc(2);
            
            // act
            var values = MetricExtensions.CollectAllValues(counter, excludeUnlabeled: true);
            
            // assert
            Assert.That(values.Count(), Is.EqualTo(2));
            Assert.That(values.Sum(), Is.EqualTo(3));
        }
        
        [Test]
        public void CollectAllValues_Extracts_All_Labeled_And_Unlabeled_Summed_Values_From_A_Histogram()
        {
            // arrange
            var histo = Prometheus.Metrics.CreateHistogram("test_histo", "", labelNames: new [] {"label1", "label2"});
            histo.Observe(1); // unlabeled
            histo.Labels("1", "2").Observe(2);
            histo.Labels("1", "2").Observe(3);
            histo.Labels("1", "3").Observe(4);
            
            // act
            var values = MetricExtensions.CollectAllSumValues(histo);
            
            // assert
            Assert.That(values.Count(), Is.EqualTo(3));
            Assert.That(values.Sum(), Is.EqualTo(10));
        }
        
        [Test]
        public void CollectAllValues_Extracts_Labeled_Summed_Values_From_A_Histogram_When_excludeUnlabeled_Is_True()
        {
            // arrange
            var histo = Prometheus.Metrics.CreateHistogram("test_histo2", "", labelNames: new [] {"label1", "label2"});
            histo.Observe(1); // unlabeled
            histo.Labels("1", "2").Observe(2);
            histo.Labels("1", "2").Observe(3);
            histo.Labels("1", "3").Observe(4);
            
            // act
            var values = MetricExtensions.CollectAllSumValues(histo, excludeUnlabeled: true);
            
            // assert
            Assert.That(values.Count(), Is.EqualTo(2));
            Assert.That(values.Sum(), Is.EqualTo(9));
        }
        
        [Test]
        public void CollectAllValues_Extracts_All_Labeled_And_Unlabeled_Count_Values_From_A_Histogram()
        {
            // arrange
            var histo = Prometheus.Metrics.CreateHistogram("test_histo3", "", labelNames: new []{ "label1", "label2"});
            histo.Observe(1); // unlabeled
            histo.Labels("1", "2").Observe(2);
            histo.Labels("1", "2").Observe(3);
            histo.Labels("1", "3").Observe(4);
            
            // act
            var values = MetricExtensions.CollectAllCountValues(histo);
            
            // assert
            Assert.That(values.Count(), Is.EqualTo(3));
            Assert.That(values.Sum(x => (long)x), Is.EqualTo(4));
        }
    }
}