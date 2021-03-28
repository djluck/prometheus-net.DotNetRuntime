using System;
using System.Diagnostics.Tracing;
using NUnit.Framework;
using Prometheus.DotNetRuntime.EventListening;
using Prometheus.DotNetRuntime.EventListening.Parsers;

namespace Prometheus.DotNetRuntime.Tests.EventListening
{
    [TestFixture]
    public class IEventParserTests
    {
        [Test]
        public void Given_A_Parser_Implements_One_Or_More_IEvents_Interface_Then_Can_Get_Appropriate_Levels()
        {
            IEventListener gcParser = new GcEventParser();
            Assert.That(gcParser.SupportedLevels, Is.EquivalentTo(new[] { EventLevel.Informational, EventLevel.Verbose }));
        }
        
        [Test]
        public void Given_A_Parser_Implements_ICounterEvents_Then_Can_Get_Appropriate_Levels()
        {
            IEventListener runtimeEventParser = new RuntimeEventParser();
            Assert.That(runtimeEventParser.SupportedLevels, Is.EquivalentTo(new[] { EventLevel.LogAlways }));
        }
        
        [Test]
        public void Given_A_Parser_Only_Implements_IEvents_Returns_No_Levels()
        {
            IEventListener noEventsParser = new TestParserNoEvents();
            Assert.That(noEventsParser.SupportedLevels, Is.Empty);
        }

        private class TestParserNoEvents : IEventParser<TestParserNoEvents>, IEvents
        {
            public Guid EventSourceGuid { get; }
            public EventKeywords Keywords { get; }
            public void ProcessEvent(EventWrittenEventArgs e)
            {
                throw new NotImplementedException();
            }
        }
    }
}