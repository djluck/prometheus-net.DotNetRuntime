using System;
using System.Threading;
using NUnit.Framework;
using Prometheus.DotNetRuntime.EventListening.Parsers;

namespace Prometheus.DotNetRuntime.Tests.EventListening.Parsers
{
    [TestFixture]
    public class SystemRuntimeCounterTests : EventListenerIntegrationTestBase<RuntimeEventParser>
    {
        [Test]
        public void TestEvent()
        {
            var resetEvent = new AutoResetEvent(false);
            Parser.AllocRate += e =>
            {
                resetEvent.Set();
                Assert.That(e.IncrementedBy, Is.GreaterThan(0));
            };

            Assert.IsTrue(resetEvent.WaitOne(TimeSpan.FromSeconds(10)));
        } 
        
        protected override RuntimeEventParser CreateListener()
        {
            return new ();
        }
    }
}