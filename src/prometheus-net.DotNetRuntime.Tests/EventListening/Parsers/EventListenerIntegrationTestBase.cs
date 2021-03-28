using System;
using System.Diagnostics.Tracing;
using System.Threading;
using NUnit.Framework;
using Prometheus.DotNetRuntime.EventListening;

namespace Prometheus.DotNetRuntime.Tests.EventListening.Parsers
{
    [TestFixture]
    public abstract class EventListenerIntegrationTestBase<TEventListener> 
        where TEventListener : IEventListener
    {
        private DotNetEventListener _eventListener;
        protected TEventListener Parser { get; private set; }

        [SetUp]
        public void SetUp()
        {
            Parser = CreateListener();
            _eventListener = new DotNetEventListener(Parser, EventLevel.LogAlways, new DotNetEventListener.GlobalOptions{ ErrorHandler = ex => Assert.Fail($"Unexpected exception occurred: {ex}")});
            
            // wait for event listener thread to spin up
            while (!_eventListener.StartedReceivingEvents)
            {
                Thread.Sleep(10); 
                Console.Write("Waiting.. ");
                
            }
            Console.WriteLine("EventListener should be active now.");
        }

        [TearDown]
        public void TearDown()
        {
            Console.WriteLine("Disposing event listener..");
            _eventListener.Dispose();
        }

        protected abstract TEventListener CreateListener();
    }
}