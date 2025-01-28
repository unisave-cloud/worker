using System.IO;

namespace UnisaveWorker.Metrics.Memory
{
    public class MemoryUsageGauge : PrometheusMetric
    {
        public MemoryUsageGauge(string name, string help)
            : base(name, help, "gauge") { }

        protected override double GetMetricValue()
        {
            return (double)GetMemoryUsageBytes();
        }

        /// <summary>
        /// Reads process memory usage from the proper cgroup file
        /// </summary>
        public static ulong GetMemoryUsageBytes()
        {
            if (Directory.Exists("/sys/fs/cgroup/memory"))
            {
                string bytesText = File.ReadAllText(
                    "/sys/fs/cgroup/memory/memory.usage_in_bytes"
                );
                ulong bytes = ulong.Parse(bytesText);
                return bytes;
            }
            else
            {
                string bytesText = File.ReadAllText(
                    "/sys/fs/cgroup/memory.current"
                );
                ulong bytes = ulong.Parse(bytesText);
                return bytes;
            }
        }
    }
}