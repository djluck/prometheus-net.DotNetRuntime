using NUnit.Framework;
using System;
using Prometheus.DotNetRuntime.Metrics.Producers;

namespace Prometheus.DotNetRuntime.Tests.IntegrationTests
{
    [TestFixture]
    internal class Given_Exception_Events_Are_Enabled_For_Exception_Stats : IntegrationTestBase<ExceptionMetricsProducer>
    {
        protected override DotNetRuntimeStatsBuilder.Builder ConfigureBuilder(DotNetRuntimeStatsBuilder.Builder toConfigure)
        {
            return toConfigure.WithExceptionStats(CaptureLevel.Errors);
        }

        [Test]
        [MaxTime(10_000)]
        public void Will_measure_when_occurring_an_exception()
        {
            // act
            var divider = 0;
            const int numToThrow = 10;

            for (int i = 0; i < numToThrow; i++)
            {
                try
                {
                    _ = 1 / divider;
                }
                catch (DivideByZeroException)
                {
                }
            }

            // assert
            Assert.That(() => MetricProducer.ExceptionCount.Labels("System.DivideByZeroException").Value, Is.GreaterThanOrEqualTo(numToThrow).After(100, 1000));
        }
    }
    
    [TestFixture]
    internal class Given_Only_Runtime_Counters_Are_Enabled_For_Exception_Stats : IntegrationTestBase<ExceptionMetricsProducer>
    {
        protected override DotNetRuntimeStatsBuilder.Builder ConfigureBuilder(DotNetRuntimeStatsBuilder.Builder toConfigure)
        {
            return toConfigure.WithExceptionStats(CaptureLevel.Counters);
        }

        [Test]
        [MaxTime(10_000)]
        public void Will_measure_when_occurring_an_exception()
        {
            // act
            var divider = 0;
            const int numToThrow = 10;

            for (int i = 0; i < numToThrow; i++)
            {
                try
                {
                    _ = 1 / divider;
                }
                catch (DivideByZeroException)
                {
                }
            }

            // assert
            Assert.That(() => MetricProducer.ExceptionCount.Value, Is.GreaterThanOrEqualTo(numToThrow).After(3_000, 100));
        }
    }
}