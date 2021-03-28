using System;

namespace Prometheus.DotNetRuntime.EventListening.Parsers
{
    internal static class DelegateExtensions
    {
        internal static void InvokeManyTimes<T>(this Action<T> d, int count, T payload)
        {
            for (int i = 0; i < count; i++)
            {
                d(payload);
            }
        }
    }
}