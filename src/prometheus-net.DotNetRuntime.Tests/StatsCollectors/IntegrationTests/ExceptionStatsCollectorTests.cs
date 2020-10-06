using NUnit.Framework;
using Prometheus.DotNetRuntime.StatsCollectors;
using System;

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
            // act
            var divider = 0;
            
            try
            {
                _ = 1 / divider;
            }
            catch (DivideByZeroException)
            {
            }

            // assert
            Assert.That(() => StatsCollector.ExceptionCount.Labels("System.DivideByZeroException").Value, Is.EqualTo(1).After(100, 1000));
        }
    }
}