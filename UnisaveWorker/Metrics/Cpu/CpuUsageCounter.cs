using System;
using System.IO;
using System.Linq;

namespace UnisaveWorker.Metrics.Cpu
{
    /// <summary>
    /// Cumulative system CPU time consumed in seconds
    /// </summary>
    public class CpuUsageCounter : PrometheusMetric
    {
        public CpuUsageCounter(string name, string help = null)
            : base(name, help, "counter") { }

        protected override double GetMetricValue()
        {
            return PerformMeasurement();
        }

        public static double PerformMeasurement()
        {
            if (Directory.Exists("/sys/fs/cgroup/cpu"))
            {
                string nsText = File.ReadAllText(
                    "/sys/fs/cgroup/cpu/cpuacct.usage"
                );
                ulong ns = ulong.Parse(nsText);
                return ns * 1e-9;
            }
            else
            {
                string line = File.ReadLines(
                    "/sys/fs/cgroup/cpu.stat"
                ).FirstOrDefault(l => l.StartsWith("usage_usec"));

                if (line == null)
                    throw new Exception(
                        "usage_usec not found in /sys/fs/cgroup/cpu.stat"
                    );

                string usText = line.Trim().Split(' ')[1];
                ulong us = ulong.Parse(usText);
                return us * 1e-6;
            }
        }
    }
}