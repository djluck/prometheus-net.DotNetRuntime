using NUnit.Framework;
using Prometheus.DotNetRuntime.Metrics.Producers.Util;

namespace Prometheus.DotNetRuntime.Tests.Metrics.Producers.Util
{
    public class StringExtensionsTests
    {
        [TestCase("", "")]
        [TestCase("myGreatVariableName", "my_great_variable_name")]
        [TestCase("my_great_variable_name", "my_great_variable_name")]
        [TestCase("MyGreatVariableName", "my_great_variable_name")]
        public void ToSnakeCase_Should_Convert_To_Snake_Case(string given, string expected)
        {
            Assert.AreEqual(given.ToSnakeCase(), expected);
        }
    }
}
