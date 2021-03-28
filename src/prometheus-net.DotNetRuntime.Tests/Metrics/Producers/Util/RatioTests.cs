using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Prometheus.DotNetRuntime.Metrics.Producers.Util;

namespace Prometheus.DotNetRuntime.Tests.Metrics.Producers.Util
{
    [TestFixture]
    public class RatioTests
    {
        private MetricFactory _metricFactory;

        [SetUp]
        public void SetUp()
        {
            _metricFactory = Prometheus.Metrics.WithCustomRegistry(Prometheus.Metrics.NewCustomRegistry());
        }
        
        [Test]
        public void CalculateConsumedRatio_returns_zero_if_no_time_has_been_consumed()
        {
            // arrange
            var ratio = Arrange_ratio(TimeSpan.Zero, TimeSpan.FromMilliseconds(100));

            Assert.That(ratio.CalculateConsumedRatio(0.0), Is.EqualTo(0));
        }
        
        [Test]
        [TestCase(1.0, 0.1)]
        [TestCase(5.0, 0.5)]
        [TestCase(10.0, 1.0)]
        public void CalculateConsumedRatio_returns_ratio_if_time_has_been_consumed(double secondsConsumedByEvents, double expectedRatio)
        {
            var ratio = Arrange_ratio(TimeSpan.Zero, TimeSpan.FromSeconds(10));

            Assert.That(ratio.CalculateConsumedRatio(secondsConsumedByEvents), Is.EqualTo(expectedRatio));
        }
        
        [Test]
        public void CalculateConsumedRatio_accounts_for_initial_offset_consumption()
        {
            var ratio = Arrange_ratio(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(10));

            Assert.That(ratio.CalculateConsumedRatio(0.5), Is.EqualTo(0.25));
        }
        
        [Test]
        public void CalculateConsumedRatio_stores_previous_process_and_event_time_consumption()
        {
            // arrange
            var ratio = Arrange_ratio(TimeSpan.Zero, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(8));

            // act + assert
            Assert.That(ratio.CalculateConsumedRatio(0.99), Is.EqualTo(0.99));
            Assert.That(ratio.CalculateConsumedRatio(0.99 + 0.1), Is.EqualTo(0.025).Within(0.000001));
            Assert.That(ratio.CalculateConsumedRatio(0.99 + 0.1 + 0.5), Is.EqualTo(0.1666666).Within(0.000001));
        }
        
        [Test]
        public void CalculateConsumedRatio_returns_zero_if_negative_time_has_been_consumed()
        {
            // arrange
            var ratio = Arrange_ratio(TimeSpan.Zero, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2));

            // act + assert
            Assert.That(ratio.CalculateConsumedRatio(0.5), Is.EqualTo(0.5));
            Assert.That(ratio.CalculateConsumedRatio(0.49), Is.EqualTo(0.0));
        }
        
        [Test]
        public void CalculateConsumedRatio_truncates_the_value_if_more_than_100_percent_has_been_consumed()
        {
            // arrange
            var ratio = Arrange_ratio(TimeSpan.Zero, TimeSpan.FromSeconds(1));

            // act + assert
            Assert.That(ratio.CalculateConsumedRatio(1.1), Is.EqualTo(1.0));
        }
        
        [Test]
        public void CalculateConsumedRatio_can_extract_the_consumed_event_time_from_an_unlabeled_counter()
        {
            // arrange
            var ratio = Arrange_ratio(TimeSpan.Zero, TimeSpan.FromSeconds(5));
            var counter = _metricFactory.CreateCounter("event_time_total_seconds", "");
            counter.Inc(2.5);
            
            // act + assert
            Assert.That(ratio.CalculateConsumedRatio(counter), Is.EqualTo(0.5));
        }
        
        [Test]
        public void CalculateConsumedRatio_can_extract_the_consumed_event_time_from_a_labeled_counter()
        {
            // arrange
            var ratio = Arrange_ratio(TimeSpan.Zero, TimeSpan.FromSeconds(5));
            var counter = _metricFactory.CreateCounter("event_time_total_seconds", "", "label_1", "label_2");
            counter.Labels("a", "b").Inc(1.0);
            counter.Labels("a", "c").Inc(0.25);
            counter.Labels("d", "e").Inc(1.25);
            
            // act + assert
            Assert.That(ratio.CalculateConsumedRatio(counter), Is.EqualTo(0.5));
        }
        
        [Test]
        public void CalculateConsumedRatio_can_extract_the_consumed_event_time_from_an_unlabeled_histogram()
        {
            // arrange
            var ratio = Arrange_ratio(TimeSpan.Zero, TimeSpan.FromSeconds(5));
            var histo = _metricFactory.CreateHistogram("event_time_seconds", "");
            histo.Observe(1.5);
            histo.Observe(1.0);
            
            // act + assert
            Assert.That(ratio.CalculateConsumedRatio(histo), Is.EqualTo(0.5));
        }
        
        [Test]
        public void CalculateConsumedRatio_can_extract_the_consumed_event_time_from_a_labeled_histogram()
        {
            // arrange
            var ratio = Arrange_ratio(TimeSpan.Zero, TimeSpan.FromSeconds(5));
            var histo = _metricFactory.CreateHistogram("event_time_total_seconds", "", labelNames: new [] {"label1", "label2"});
            histo.Labels("a", "b").Observe(1.0);
            histo.Labels("a", "c").Observe(0.25);
            histo.Labels("d", "e").Observe(1.25);
            
            // act + assert
            Assert.That(ratio.CalculateConsumedRatio(histo), Is.EqualTo(0.5));
        }
        
        [Test]
        public void ProcessTime_CalculateConsumedRatio_initalises_using_the_current_time_at_creation()
        {
            // arrange
            var processTimeRatio = Ratio.ProcessTime();
            Thread.Sleep(100);

            // act + assert
            Assert.That(processTimeRatio.CalculateConsumedRatio(0.01), Is.EqualTo(0.1).Within(0.05));
        }

        private Ratio Arrange_ratio(params TimeSpan[] processorTimes)
        {
            var enumerator = processorTimes.AsEnumerable().GetEnumerator();
            
            return new Ratio(() =>
            {
                if (!enumerator.MoveNext())
                {
                    Assert.Fail("Did not pass sufficient processor times!");
                }

                return enumerator.Current;
            });
        }
    }
}