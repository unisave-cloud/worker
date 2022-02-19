using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Watchdog.Metrics
{
    public abstract class PrometheusMetric
    {
        private readonly Dictionary<string, string> labels
            = new Dictionary<string, string>();

        public string MetricName { get; }
        public string MetricHelp { get; }
        public string MetricType { get; }

        protected PrometheusMetric(string name, string help, string type)
        {
            MetricName = name ?? throw new ArgumentNullException(nameof(name));
            MetricHelp = help;
            MetricType = type;
        }
        
        public string this[string label]
        {
            set => labels[label] = value;
        }

        protected abstract double GetMetricValue();
        
        public void ToPrometheusTextFormat(StringBuilder sb)
        {
            if (!string.IsNullOrWhiteSpace(MetricHelp))
                sb.AppendLine($"# HELP {MetricName} {MetricHelp}");
            
            if (!string.IsNullOrWhiteSpace(MetricType))
                sb.AppendLine($"# TYPE {MetricName} {MetricType}");

            sb.Append(MetricName);

            if (labels.Count > 0)
            {
                sb.Append("{");
                bool prependComma = false;
                foreach (var pair in labels)
                {
                    if (prependComma)
                        sb.Append(",");

                    sb.Append(pair.Key);
                    sb.Append("=\"");
                    sb.Append(pair.Value.Replace("\"", "\\\""));
                    sb.Append("\"");
                    
                    prependComma = true;
                }
                sb.Append("}");
            }
            
            sb.Append(" ");
            sb.AppendLine(
                GetMetricValue().ToString(CultureInfo.InvariantCulture)
            );
        }
    }
}