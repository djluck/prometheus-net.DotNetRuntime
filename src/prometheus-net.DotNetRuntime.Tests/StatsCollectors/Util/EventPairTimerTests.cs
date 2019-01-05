using System;
using System.Collections.ObjectModel;
using System.Diagnostics.Tracing;
using NUnit.Framework;
using Prometheus.DotNetRuntime.StatsCollectors.Util;
using Fasterflect;

namespace Prometheus.DotNetRuntime.Tests.StatsCollectors.Util
{
    [TestFixture]
    public class EventPairTimerTests
    {
        private const int EventIdStart = 1, EventIdEnd = 2;
        private EventPairTimer<long> _eventPairTimer;

        [SetUp]
        public void SetUp()
        {
            _eventPairTimer = new EventPairTimer<long>(EventIdStart, EventIdEnd, x => (long)x.Payload[0]);
        }

        [Test]
        public void TryGetEventPairDuration_ignores_events_that_its_not_configured_to_look_for()
        {
            var nonMonitoredEvent = CreateEventWrittenEventArgs(3);
            Assert.That(_eventPairTimer.TryGetEventPairDuration(nonMonitoredEvent, out var duration), Is.False);
            Assert.That(duration, Is.EqualTo(TimeSpan.Zero));
        }
        
        [Test]
        public void TryGetEventPairDuration_ignores_end_events_if_it_never_saw_the_start_event()
        {
            var nonMonitoredEvent = CreateEventWrittenEventArgs(EventIdEnd, payload: 1L);
            Assert.That(_eventPairTimer.TryGetEventPairDuration(nonMonitoredEvent, out var duration), Is.False);
            Assert.That(duration, Is.EqualTo(TimeSpan.Zero));
        }
        
        [Test]
        public void TryGetEventPairDuration_calculates_duration_between_configured_events()
        {
            // arrange
            var now = DateTime.UtcNow;
            var startEvent = CreateEventWrittenEventArgs(EventIdStart, now, payload: 1L);
            Assert.That(_eventPairTimer.TryGetEventPairDuration(startEvent, out var _), Is.False);
            var endEvent = CreateEventWrittenEventArgs(EventIdEnd, now.AddMilliseconds(100), payload: 1L);
            
            // act
            Assert.That(_eventPairTimer.TryGetEventPairDuration(endEvent, out var duration), Is.True);
            Assert.That(duration.TotalMilliseconds, Is.EqualTo(100));
        }
        
        [Test]
        public void TryGetEventPairDuration_calculates_duration_between_configured_events_that_occur_simultaneously()
        {
            // arrange
            var now = DateTime.UtcNow;
            var startEvent = CreateEventWrittenEventArgs(EventIdStart, now, payload: 1L);
            Assert.That(_eventPairTimer.TryGetEventPairDuration(startEvent, out var _), Is.False);
            var endEvent = CreateEventWrittenEventArgs(EventIdEnd, now, payload: 1L);
            
            // act
            Assert.That(_eventPairTimer.TryGetEventPairDuration(endEvent, out var duration), Is.True);
            Assert.That(duration, Is.EqualTo(TimeSpan.Zero));
        }
        
        [Test]
        public void TryGetEventPairDuration_calculates_duration_between_multiple_out_of_order_configured_events()
        {
            // arrange 
            var now = DateTime.UtcNow;
            var startEvent1 = CreateEventWrittenEventArgs(EventIdStart, now, payload: 1L);
            var endEvent1 = CreateEventWrittenEventArgs(EventIdEnd, now.AddMilliseconds(300), payload: 1L);
            var startEvent2 = CreateEventWrittenEventArgs(EventIdStart, now, payload: 2L);
            var endEvent2 = CreateEventWrittenEventArgs(EventIdEnd, now.AddMilliseconds(200), payload: 2L);
            var startEvent3 = CreateEventWrittenEventArgs(EventIdStart, now, payload: 3L);
            var endEvent3 = CreateEventWrittenEventArgs(EventIdEnd, now.AddMilliseconds(100), payload: 3L);

            _eventPairTimer.TryGetEventPairDuration(startEvent1, out var _);
            _eventPairTimer.TryGetEventPairDuration(startEvent2, out var _);
            _eventPairTimer.TryGetEventPairDuration(startEvent3, out var _);
            
            // act
            Assert.That(_eventPairTimer.TryGetEventPairDuration(endEvent3, out var event3Duration), Is.True);
            Assert.That(_eventPairTimer.TryGetEventPairDuration(endEvent2, out var event2Duration), Is.True);
            Assert.That(_eventPairTimer.TryGetEventPairDuration(endEvent1, out var event1Duration), Is.True);
            
            Assert.That(event1Duration.TotalMilliseconds, Is.EqualTo(300));
            Assert.That(event2Duration.TotalMilliseconds, Is.EqualTo(200));
            Assert.That(event3Duration.TotalMilliseconds, Is.EqualTo(100));
        }

        private EventWrittenEventArgs CreateEventWrittenEventArgs(int eventId, DateTime? timestamp = null, params object[] payload)
        {
            var args = (EventWrittenEventArgs)typeof(EventWrittenEventArgs).CreateInstance(new []{ typeof(EventSource)}, Flags.NonPublic | Flags.Instance, new object[] { null});
            args.SetPropertyValue("EventId", eventId);
            args.SetPropertyValue("Payload", new ReadOnlyCollection<object>(payload));
            
            if (timestamp.HasValue)
            {
                args.SetPropertyValue("TimeStamp", timestamp.Value);
            }
            
            return args;
        }
    }
}