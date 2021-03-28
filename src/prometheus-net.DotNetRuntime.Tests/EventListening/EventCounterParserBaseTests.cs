using System;
using System.Diagnostics.Tracing;
using NUnit.Framework;
using Prometheus.DotNetRuntime.EventListening;

namespace Prometheus.DotNetRuntime.Tests.EventListening
{
    [TestFixture]
    public class Given_An_Implementation_Of_EventCounterParserBaseTest_That_Has_No_Decorated_Events
    {
        [Test]
        public void When_Attributes_Are_Missing_From_Interface_Events_Then_Throw_Exception()
        {
            var ex = Assert.Throws<Exception>(() => new NoAttributesEventCounterParser());
            Assert.That(ex, Has.Message.Contains("All events part of an ICounterEvents interface require a [CounterNameAttribute] attribute. Events without attribute: TestIncrementingCounter, TestMeanCounter."));
        }

        public class NoAttributesEventCounterParser : EventCounterParserBase<NoAttributesEventCounterParser>, TestCounters
        {
            public override Guid EventSourceGuid { get; }
            public override EventKeywords Keywords { get; }
            public override int RefreshIntervalSeconds { get; set; }
            
            public event Action<IncrementingCounterValue> TestIncrementingCounter;
            public event Action<MeanCounterValue> TestMeanCounter;
            public event Action UnrelatedEvent;
        }
    }
    
    [TestFixture]
    public class Given_An_Implementation_Of_EventCounterParserBaseTest_That_Has_Decorated_Events
    {
        private DummyTypeEventCounterParser _parser;

        [SetUp]
        public void SetUp()
        {
            _parser = new DummyTypeEventCounterParser();
        }
        
        [Test]
        public void When_An_Empty_Event_Is_Passed_Then_No_Exception_Occurs_And_No_Event_Is_Raised()
        {
            // arrange
            var eventAssertionMean = TestHelpers.ArrangeEventAssertion<MeanCounterValue>(h => _parser.TestMeanCounter += h);
            var eventAssertionIncr = TestHelpers.ArrangeEventAssertion<IncrementingCounterValue>(h => _parser.TestIncrementingCounter += h);
            
            // act
            _parser.ProcessEvent(TestHelpers.CreateCounterEventWrittenEventArgs());
            
            // assert
            Assert.IsFalse(eventAssertionIncr.Fired);
            Assert.IsFalse(eventAssertionMean.Fired);
        }
        
        [Test]
        public void When_A_MeanCounter_Is_Passed_For_A_Matching_MeanCounterValue_Event_Then_The_Event_Is_Fired()
        {
            // arrange
            var eventAssertion = TestHelpers.ArrangeEventAssertion<MeanCounterValue>(h => _parser.TestMeanCounter += h);
            var e = TestHelpers.CreateCounterEventWrittenEventArgs(
                ("CounterType", "Mean"),
                ("Name", "test-mean-counter"),
                ("Mean", 5.0),
                ("Count", 1)
            );
            
            // act
            _parser.ProcessEvent(e);
            
            // assert
            Assert.That(eventAssertion.Fired, Is.True);
            Assert.That(eventAssertion.LastEvent.Mean, Is.EqualTo(5.0));
            Assert.That(eventAssertion.LastEvent.Count, Is.EqualTo(1));
        }

        [Test]
        public void When_A_IncrementingCounter_Is_Passed_For_A_Matching_IncrementingCounterValue_Event_Then_The_Event_Is_Fired()
        {
            // arrange
            var eventAssertion = TestHelpers.ArrangeEventAssertion<IncrementingCounterValue>(h => _parser.TestIncrementingCounter += h);
            var e = TestHelpers.CreateCounterEventWrittenEventArgs(
                ("CounterType", "Sum"),
                ("Name", "test-incrementing-counter"),
                ("Increment", 10.0)
            );
            
            // act
            _parser.ProcessEvent(e);
            
            // assert
            Assert.That(eventAssertion.Fired, Is.True);
            Assert.That(eventAssertion.LastEvent.IncrementedBy, Is.EqualTo(10.0));
        }
        
        [Test]
        public void When_A_IncrementingCounter_Is_Passed_For_A_Mismatching_MeanCounterValue_Event_Then_An_Exception_Is_Thrown()
        {
            // arrange
            var meanEventAssertion = TestHelpers.ArrangeEventAssertion<MeanCounterValue>(h => _parser.TestMeanCounter += h);
            var incrEventAssertion = TestHelpers.ArrangeEventAssertion<IncrementingCounterValue>(h => _parser.TestIncrementingCounter += h);
            
            var e = TestHelpers.CreateCounterEventWrittenEventArgs(
                ("CounterType", "Mean"),
                // refers to the incrementing counter
                ("Name", "test-incrementing-counter"),
                ("Mean", 5.0),
                ("Count", 1)
            );
            
            // act
            Assert.Throws<MismatchedCounterTypeException>(() => _parser.ProcessEvent(e));
        }

        public class DummyTypeEventCounterParser : EventCounterParserBase<DummyTypeEventCounterParser>, TestCounters
        {
            public override Guid EventSourceGuid { get; }
            public override EventKeywords Keywords { get; }
            public override int RefreshIntervalSeconds { get; set; }
            
            [CounterName("test-incrementing-counter")]
            public event Action<IncrementingCounterValue> TestIncrementingCounter;
            [CounterName("test-mean-counter")]
            public event Action<MeanCounterValue> TestMeanCounter;
        }
    }
    
    public interface TestCounters : ICounterEvents
    {
#pragma disable warning
        public event Action<IncrementingCounterValue> TestIncrementingCounter;
        public event Action<MeanCounterValue> TestMeanCounter;
#pragma warning restore
    }
}