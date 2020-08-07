using NUnit.Framework;
using Prometheus.DotNetRuntime.StatsCollectors;
using System;
using System.Threading;

namespace Prometheus.DotNetRuntime.Tests.StatsCollectors.IntegrationTests
{
    [TestFixture]
    internal class ExceptionStatsCollectorTests : StatsCollectorIntegrationTestBase<ExceptionStatsCollector>
    {
        protected override ExceptionStatsCollector CreateStatsCollector()
        {
            return new ExceptionStatsCollector();
        }

        [Test]
        public void Will_measure_when_occurring_an_exception()
        {
            // arrange
            var divider = 0;
            var exceptionMessage = string.Empty;

            // act
            try
            {
                var result = 1 / divider;
            }
            catch (Exception ex)
            {
                exceptionMessage = ex.Message;
            }

            // throwing exceptions are slower so please wait 300 ms.
            Thread.Sleep(300);

            var value = StatsCollector.ExceptionReasons.Labels(exceptionMessage).Value;

            // assert
            Assert.That(value, Is.EqualTo(1d));
        }

        [Test]
        public void Will_measure_when_not_occurring_an_exception()
        {
            // arrange
            int divider = 1;
            string exceptionMessage = string.Empty;

            // act
            try
            {
                var result = 1 / divider;
            }
            catch (DivideByZeroException ex)
            {
                exceptionMessage = ex.Message;
            }

            // throwing exceptions are slower so please wait 300 ms.
            Thread.Sleep(300);

            var value = StatsCollector.ExceptionReasons.Labels(exceptionMessage).Value;

            // assert
            Assert.That(value, Is.EqualTo(0d));
        }
    }
}