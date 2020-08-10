using NUnit.Framework;
using Prometheus.DotNetRuntime.StatsCollectors;
using System;
using System.Threading;
using System.Threading.Tasks;

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
            int divider = 0;
            string exceptionMessage = string.Empty;

            // act
            try
            {
                var result = 1 / divider;
            }
            catch (Exception ex)
            {
                exceptionMessage = ex.GetType().FullName;
            }

            // assert
            Assert.That(() => StatsCollector.ExceptionReasons.Labels(exceptionMessage).Value, Is.EqualTo(1).After(100, 1000));
        }

        [Test]
        public void Will_measure_when_not_occurring_an_exception()
        {
            // arrange
            int divider = 1;
            string exceptionMessage = string.Empty;

            // act
            var result = 1 / divider;

            // assert
            Assert.That(() => StatsCollector.ExceptionReasons.Labels(exceptionMessage).Value, Is.EqualTo(0).After(100, 1000));
        }
    }
}