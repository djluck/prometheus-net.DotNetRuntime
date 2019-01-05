using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Prometheus.DotNetRuntime.StatsCollectors.Util
{
    /// <summary>
    /// Generating tags often involves heavy use of String.Format, which takes CPU time and needlessly re-allocates 
    /// strings. Pre-generating these labels helps keep resource use to a minimum.
    /// </summary>
    public class LabelGenerator
    {
        public static Dictionary<TEnum, string> MapEnumToLabelValues<TEnum>()
        {
            var dict = new Dictionary<TEnum, string>();

            foreach (var v in Enum.GetValues(typeof(TEnum)).Cast<TEnum>())
            {
                dict.Add(v, ToSnakeCase(Enum.GetName(typeof(TEnum), v)));
            }

            return dict;
        }

        private static string ToSnakeCase(string str)
        {
            var sb = new StringBuilder();
            bool lastCharWasUpper = false;
            bool isFirst = true;
            
            foreach (var c in str)
            {
                if (char.IsUpper(c))
                {
                    if (!lastCharWasUpper && !isFirst)
                    {
                        sb.Append("_");
                    }

                    sb.Append(char.ToLower(c));
                    lastCharWasUpper = true;
                }
                else
                {
                    sb.Append(c);
                    lastCharWasUpper = false;
                }

                isFirst = false;
            }

            return sb.ToString();
        }
    }
}