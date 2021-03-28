using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Prometheus.DotNetRuntime.Metrics.Producers;

namespace Prometheus.DotNetRuntime.Tests.IntegrationTests
{
    [TestFixture]
    internal class Given_Contention_Events_Are_Enabled_For_Contention_Stats : IntegrationTestBase<ContentionMetricsProducer>
    {
        protected override DotNetRuntimeStatsBuilder.Builder ConfigureBuilder(DotNetRuntimeStatsBuilder.Builder toConfigure)
        {
            return toConfigure.WithContentionStats(CaptureLevel.Informational, SampleEvery.OneEvent);
        }
        
        [Test]
        public void Will_measure_no_contention_on_an_uncontested_lock()
        {
            // arrange
            var key = new Object();
            
            // act
            lock (key)
            {
            }
            
            // assert
            Assert.That(MetricProducer.ContentionTotal.Value, Is.EqualTo(0));
            Assert.That(MetricProducer.ContentionSecondsTotal.Value, Is.EqualTo(0)); 
        }
        
        /// <summary>
        /// This test has the potential to be flaky (due to attempting to simulate lock contention across multiple threads in the thread pool),
        /// may have to revisit this in the future..
        /// </summary>
        /// <returns></returns>
        [Test]
        [Repeat(3)]
        public async Task Will_measure_contention_on_a_contested_lock()
        {
            // arrange
            const int numThreads = 10;
            const int sleepForMs = 50;
            var key = new object();
            // Increase the min. thread pool size so that when we use Thread.Sleep, we don't run into scheduling delays
            ThreadPool.SetMinThreads(numThreads * 2, 1);

            // act
            var tasks = Enumerable.Range(1, numThreads)
                .Select(_ => Task.Run(() =>
                {
                    lock (key)
                    {
                        Thread.Sleep(sleepForMs);
                    }        
                }));
            
            await Task.WhenAll(tasks);
            
            // assert
            
            // Why -1? The first thread will not contend the lock 
            const int numLocksContended = numThreads - 1;
            Assert.That(() => MetricProducer.ContentionTotal.Value, Is.GreaterThanOrEqualTo(numLocksContended).After(3000, 10));
            
            // Pattern of expected contention times is: 50ms, 100ms, 150ms, etc.
            var expectedDelay = TimeSpan.FromMilliseconds(Enumerable.Range(1, numLocksContended).Aggregate(sleepForMs, (acc, next) => acc + (sleepForMs * next)));
            Assert.That(MetricProducer.ContentionSecondsTotal.Value, Is.EqualTo(expectedDelay.TotalSeconds).Within(sleepForMs)); 
        }
    }
    
    [TestFixture]
    internal class Given_Only_Counters_Are_Enabled_For_Contention_Stats : IntegrationTestBase<ContentionMetricsProducer>
    {
        protected override DotNetRuntimeStatsBuilder.Builder ConfigureBuilder(DotNetRuntimeStatsBuilder.Builder toConfigure)
        {
            return toConfigure.WithContentionStats(CaptureLevel.Counters, SampleEvery.OneEvent);
        }

        /// <summary>
        /// This test has the potential to be flaky (due to attempting to simulate lock contention across multiple threads in the thread pool),
        /// may have to revisit this in the future..
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task Will_measure_contention_on_a_contested_lock()
        {
            // arrange
            const int numThreads = 10;
            const int sleepForMs = 50;
            var key = new object();
            // Increase the min. thread pool size so that when we use Thread.Sleep, we don't run into scheduling delays
            ThreadPool.SetMinThreads(numThreads * 2, 1);

            // act
            var tasks = Enumerable.Range(1, numThreads)
                .Select(_ => Task.Run(() =>
                {
                    lock (key)
                    {
                        Thread.Sleep(sleepForMs);
                    }        
                }));
            
            await Task.WhenAll(tasks);
            
            // assert
            
            // Why -1? The first thread will not contend the lock 
            const int numLocksContended = numThreads - 1;
            Assert.That(() => MetricProducer.ContentionTotal.Value, Is.GreaterThanOrEqualTo(numLocksContended).After(3000, 10));
        }
    }
}