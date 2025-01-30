using System.Diagnostics;
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
            else if (File.Exists("/sys/fs/cgroup/memory.current"))
            {
                string bytesText = File.ReadAllText(
                    "/sys/fs/cgroup/memory.current"
                );
                ulong bytes = ulong.Parse(bytesText);
                return bytes;
            }
            else
            {
                // https://askubuntu.com/questions/392262/
                // how-to-monitor-the-memory-consumed-by-a-process
                int pid = Process.GetCurrentProcess().Id;
                string text = File.ReadAllText(
                    $"/proc/{pid}/statm"
                );
                string[] parts = text.Split(' ');
                // resident set size is the second number
                ulong pages = ulong.Parse(parts[1]);
                ulong bytes = pages * 4096; // page size is 4K
                return bytes;
            }
        }
    }
}