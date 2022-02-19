using System;

namespace Watchdog.Metrics.Other
{
    /// <summary>
    /// Worker instance uptime seconds
    /// </summary>
    public class UptimeCounter : PrometheusMetric
    {
        private readonly DateTime startup = DateTime.UtcNow;
        
        public UptimeCounter(string name, string help = null)
            : base(name, help, "counter") { }
        
        protected override double GetMetricValue()
        {
            return (DateTime.UtcNow - startup).TotalSeconds;
        }
    }
}