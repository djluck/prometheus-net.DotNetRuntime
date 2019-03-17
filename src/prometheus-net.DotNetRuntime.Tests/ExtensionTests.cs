using System.Linq;
using NUnit.Framework;

namespace Prometheus.DotNetRuntime.Tests
{
    [TestFixture]
    public class ExtensionTests
    {
        [Test]
        public void CollectAllValues_Extracts_All_Labeled_And_Unlabeled_Values_From_A_Counter()
        {
            // arrange
            var counter = Metrics.CreateCounter("test_counter", "", "label1", "label2");
            counter.Inc(); // unlabeled
            counter.Labels("1", "2").Inc();
            counter.Labels("1", "3").Inc(2);
            
            // act
            var values = counter.CollectAllValues();
            
            // assert
            Assert.That(values.Count(), Is.EqualTo(3));
            Assert.That(values.Sum(), Is.EqualTo(4));
        }
        
        [Test]
        public void CollectAllValues_Extracts_All_Labeled_Values_From_A_Counter_When_excludeUnlabeled_Is_True()
        {
            // arrange
            var counter = Metrics.CreateCounter("test_counter2", "", "label1", "label2");
            counter.Inc(); // unlabeled
            counter.Labels("1", "2").Inc();
            counter.Labels("1", "3").Inc(2);
            
            // act
            var values = counter.CollectAllValues(excludeUnlabeled: true);
            
            // assert
            Assert.That(values.Count(), Is.EqualTo(2));
            Assert.That(values.Sum(), Is.EqualTo(3));
        }
        
        [Test]
        public void CollectAllValues_Extracts_All_Labeled_And_Unlabeled_Summed_Values_From_A_Histogram()
        {
            // arrange
            var histo = Metrics.CreateHistogram("test_histo", "", labelNames: new [] {"label1", "label2"});
            histo.Observe(1); // unlabeled
            histo.Labels("1", "2").Observe(2);
            histo.Labels("1", "2").Observe(3);
            histo.Labels("1", "3").Observe(4);
            
            // act
            var values = histo.CollectAllSumValues();
            
            // assert
            Assert.That(values.Count(), Is.EqualTo(3));
            Assert.That(values.Sum(), Is.EqualTo(10));
        }
        
        [Test]
        public void CollectAllValues_Extracts_Labeled_Summed_Values_From_A_Histogram_When_excludeUnlabeled_Is_True()
        {
            // arrange
            var histo = Metrics.CreateHistogram("test_histo2", "", labelNames: new [] {"label1", "label2"});
            histo.Observe(1); // unlabeled
            histo.Labels("1", "2").Observe(2);
            histo.Labels("1", "2").Observe(3);
            histo.Labels("1", "3").Observe(4);
            
            // act
            var values = histo.CollectAllSumValues(excludeUnlabeled: true);
            
            // assert
            Assert.That(values.Count(), Is.EqualTo(2));
            Assert.That(values.Sum(), Is.EqualTo(9));
        }
        
        [Test]
        public void CollectAllValues_Extracts_All_Labeled_And_Unlabeled_Count_Values_From_A_Histogram()
        {
            // arrange
            var histo = Metrics.CreateHistogram("test_histo3", "", labelNames: new []{ "label1", "label2"});
            histo.Observe(1); // unlabeled
            histo.Labels("1", "2").Observe(2);
            histo.Labels("1", "2").Observe(3);
            histo.Labels("1", "3").Observe(4);
            
            // act
            var values = histo.CollectAllCountValues();
            
            // assert
            Assert.That(values.Count(), Is.EqualTo(3));
            Assert.That(values.Sum(x => (long)x), Is.EqualTo(4));
        }
    }
}