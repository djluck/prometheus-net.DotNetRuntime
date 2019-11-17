using System.Collections.Generic;

namespace Prometheus.DotNetRuntime
{
    public static class Extensions
    {
        public static void AddOrReplace<T>(this ISet<T> s, T toAddOrReplace)
        {
            if (!s.Add(toAddOrReplace))
            {
                s.Remove(toAddOrReplace);
                s.Add(toAddOrReplace);
            }
        }
    }
}