using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics.Tracing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Prometheus.DotNetRuntime
{
    internal static class Extensions
    {
        internal static void AddOrReplace<T>(this ISet<T> s, T toAddOrReplace)
        {
            if (!s.Add(toAddOrReplace))
            {
                s.Remove(toAddOrReplace);
                s.Add(toAddOrReplace);
            }
        }

        internal static void TryAddSingletonEnumerable<TService, TImplementation>(this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<TService, TImplementation>());
        }
        
        internal static EventLevel ToEventLevel(this CaptureLevel level)
        {
            return (EventLevel) (int)level;
        }
        
        internal static CaptureLevel ToCaptureLevel(this EventLevel level)
        {
            return (CaptureLevel) (int)level;
        }
    }
}