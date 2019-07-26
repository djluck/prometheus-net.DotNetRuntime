using System.Text;

namespace Prometheus.DotNetRuntime.StatsCollectors.Util
{
    public static class StringExtensions
    {
        public static string ToSnakeCase(this string str)
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