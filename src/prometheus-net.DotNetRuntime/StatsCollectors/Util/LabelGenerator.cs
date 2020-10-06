using System;
using System.Collections.Generic;
using System.Linq;

namespace Prometheus.DotNetRuntime.StatsCollectors.Util
{
    /// <summary>
    /// Generating tags often involves heavy use of String.Format, which takes CPU time and needlessly re-allocates 
    /// strings. Pre-generating these labels helps keep resource use to a minimum.
    /// </summary>
    public class LabelGenerator
    {
        public static Dictionary<TEnum, string> MapEnumToLabelValues<TEnum>()
            where TEnum : Enum
        {
            return Enum.GetValues(typeof(TEnum)).Cast<TEnum>()
                .ToDictionary(k => k, v => Enum.GetName(typeof(TEnum), v).ToSnakeCase());
        }
    }
}