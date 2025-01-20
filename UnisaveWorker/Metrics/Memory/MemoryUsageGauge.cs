using System.IO;

namespace UnisaveWorker.Metrics.Memory
{
    public class MemoryUsageGauge : PrometheusMetric
    {
        public MemoryUsageGauge(string name, string help)
            : base(name, help, "gauge") { }

        protected override double GetMetricValue()
        {
            if (Directory.Exists("/sys/fs/cgroup/memory"))
            {
                string bytesText = File.ReadAllText(
                    "/sys/fs/cgroup/memory/memory.usage_in_bytes"
                );
                ulong bytes = ulong.Parse(bytesText);
                return (double)bytes;
            }
            else
            {
                string bytesText = File.ReadAllText(
                    "/sys/fs/cgroup/memory.current"
                );
                ulong bytes = ulong.Parse(bytesText);
                return (double)bytes;
            }
        }
    }
}