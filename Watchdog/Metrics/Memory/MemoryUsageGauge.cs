using System.IO;

namespace Watchdog.Metrics.Memory
{
    public class MemoryUsageGauge : PrometheusMetric
    {
        public MemoryUsageGauge(string name, string help)
            : base(name, help, "gauge") { }

        protected override double GetMetricValue()
        {
            string bytesText = File.ReadAllText(
                "/sys/fs/cgroup/memory/memory.usage_in_bytes"
            );
            ulong bytes = ulong.Parse(bytesText);
            return (double) bytes;
        }
    }
}