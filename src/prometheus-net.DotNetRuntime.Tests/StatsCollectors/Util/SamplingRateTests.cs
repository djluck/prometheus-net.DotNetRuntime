using System;
using NUnit.Framework;
using Prometheus.DotNetRuntime.StatsCollectors.Util;

namespace Prometheus.DotNetRuntime.Tests.StatsCollectors.Util
{
    [TestFixture]
    public class SamplingRateTests
    {
        [Test]
        [TestCase(SampleEvery.OneEvent, 1)]
        [TestCase(SampleEvery.TwoEvents, 2)]
        [TestCase(SampleEvery.FiveEvents, 5)]
        [TestCase(SampleEvery.TenEvents, 10)]
        [TestCase(SampleEvery.TwentyEvents, 20)]
        [TestCase(SampleEvery.FiftyEvents, 50)]
        [TestCase(SampleEvery.HundredEvents, 100)]
        public void SampleEvery_Reflects_The_Ratio_Of_Every_100_Events_That_Will_Be_Sampled(SampleEvery samplingRate, int expected)
        {
            var sr = new SamplingRate(samplingRate);
            Assert.That(sr.SampleEvery, Is.EqualTo(expected));
        }

        [Test]
        [TestCase(SampleEvery.OneEvent, 1000)]
        [TestCase(SampleEvery.TwoEvents, 500)]
        [TestCase(SampleEvery.FiveEvents, 200)]
        [TestCase(SampleEvery.TenEvents, 100)]
        [TestCase(SampleEvery.TwentyEvents, 50)]
        [TestCase(SampleEvery.FiftyEvents, 20)]
        [TestCase(SampleEvery.HundredEvents, 10)]
        public void Given_1000_Events_ShouldSampleEvent_Returns_True_Every_Nth_Event(SampleEvery samplingRate, int expectedEvents)
        {
            var eventsSampled = 0;
            var sr = new SamplingRate(samplingRate);
            
            for (int i = 0; i < 1_000; i++)
            {
                if (sr.ShouldSampleEvent())
                {
                    eventsSampled++;
                }
            }
            
            Assert.That(eventsSampled, Is.EqualTo(expectedEvents));
        }
    }
}