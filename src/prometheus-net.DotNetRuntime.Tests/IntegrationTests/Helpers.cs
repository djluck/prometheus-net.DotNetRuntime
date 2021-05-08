using System;
using System.Linq.Expressions;

namespace Prometheus.DotNetRuntime.Tests.IntegrationTests
{
    public static class RuntimeEventHelper
    {
        public static void CompileMethods(Expression<Func<int>> toCompile, int times = 100)
        {
            for (int i = 0; i < 100; i++)
            {
                toCompile.Compile();
            }
        }
    }
}