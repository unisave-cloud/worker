using System;
using System.IO;
using System.Linq;

namespace UnisaveWorker.Metrics.Network
{
    public class NetstatGauge : PrometheusMetric
    {
        private readonly string netstatGroup;
        private readonly string netstatValue;
        
        public NetstatGauge(
            string name,
            string help,
            string netstatGroup,
            string netstatValue
        )
            : base(name, help, "gauge")
        {
            this.netstatGroup = netstatGroup;
            this.netstatValue = netstatValue;
        }

        protected override double GetMetricValue()
        {
            string[] lines = File.ReadLines("/proc/net/netstat")
                .Where(line => line.StartsWith(netstatGroup + ":"))
                .ToArray();

            if (lines.Length != 2)
                return 0;

            string[] keys = lines[0].Split();
            string[] values = lines[1].Split();

            int i = Array.IndexOf(keys, netstatValue);

            if (i == -1)
                return 0;
            
            if (!ulong.TryParse(values[i], out ulong value))
                return 0;

            return (double) value;
        }
    }
}