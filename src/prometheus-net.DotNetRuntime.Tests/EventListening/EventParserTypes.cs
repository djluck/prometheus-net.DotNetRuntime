using System.Diagnostics.Tracing;
using NUnit.Framework;
using NUnit.Framework.Internal.Execution;
using Prometheus.DotNetRuntime.EventListening;
using Prometheus.DotNetRuntime.EventListening.Parsers;

namespace Prometheus.DotNetRuntime.Tests.EventListening
{
    [TestFixture]
    public class EventParserTypesTests
    {
        [Test]
        public void Given_A_Type_That_Implements_All_IEvent_Interfaces_When_Calling_GetEventInterfaces_Then_Returns_All_Interfaces_Except_IEvents()
        {
            var interfaces = EventParserTypes.GetEventInterfaces(typeof(AllEvents));
            
            Assert.That(interfaces, Is.EquivalentTo(new[]
            {
                typeof(AllEvents.Events.Verbose),
                typeof(AllEvents.Events.Info),
                typeof(AllEvents.Events.Warning),
                typeof(AllEvents.Events.Error),
                typeof(AllEvents.Events.Always),
                typeof(AllEvents.Events.Critical),
                typeof(AllEvents.Events.Counters)
            }));
        }
        
        [Test]
        public void Given_A_Type_That_Implements_All_IEvent_Interfaces_When_Calling_GetLevelsFromType_Then_Returns_All_Interfaces_Except_IEvents()
        {
            var levels = EventParserTypes.GetLevelsFromParser(typeof(AllEvents));
            
            Assert.That(levels, Is.EquivalentTo(new[]
            {
                EventLevel.LogAlways,
                EventLevel.Verbose,
                EventLevel.Informational,
                EventLevel.Warning,
                EventLevel.Error,
                EventLevel.Critical
            }));
        }
        
        [Test]
        public void When_Calling_GetEventParsers_Then_Returns_All_Event_Parsers_Defined_In_The_DotNetRuntime_Library()
        {
            var parsers = EventParserTypes.GetEventParsers();
            
            Assert.That(parsers, Is.SupersetOf(new[]
            {
                typeof(GcEventParser),
                typeof(JitEventParser),
                typeof(ThreadPoolEventParser),
                typeof(RuntimeEventParser),
                typeof(ContentionEventParser),
                typeof(ExceptionEventParser)
            }));
        }

        public class AllEvents : AllEvents.Events.Verbose, AllEvents.Events.Info, AllEvents.Events.Warning, AllEvents.Events.Error, AllEvents.Events.Always, AllEvents.Events.Counters, AllEvents.Events.Critical, IEvents
        {
            public static class Events
            {
                public interface Verbose : IVerboseEvents{}
                public interface Info : IInfoEvents{}
                public interface Warning : IWarningEvents {}
                public interface Error : IErrorEvents{}
                public interface Always : IAlwaysEvents{}
                public interface Critical : ICriticalEvents{}
                public interface Counters : ICounterEvents{}
            }
        }
    }
}