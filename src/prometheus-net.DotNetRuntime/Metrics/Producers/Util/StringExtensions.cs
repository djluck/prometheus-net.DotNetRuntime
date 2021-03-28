using System.Text;

namespace Prometheus.DotNetRuntime.Metrics.Producers.Util
{
    public static class StringExtensions
    {
        public static string ToSnakeCase(this string str)
        {
            var sb = new StringBuilder();
            var lastCharWasUpper = false;
            
            for(var i = 0 ; i < str.Length ; i++)
            {
                if (char.IsUpper(str[i]))
                {
                    if (!lastCharWasUpper && i != 0)
                    {
                        sb.Append("_");
                    }

                    sb.Append(char.ToLower(str[i]));
                    lastCharWasUpper = true;
                }
                else
                {
                    sb.Append(str[i]);
                    lastCharWasUpper = false;
                }
            }

            return sb.ToString();
        }
    }
}