using System;
using System.Text;
using Watchdog.Metrics.Cpu;

namespace Watchdog.Metrics
{
    public class MetricsManager : IDisposable
    {
        private readonly CpuUtilizationTracker cpuUtilizationTracker;
        
        private readonly CpuUsageGauge cpuUsageGauge;
        private readonly CpuUtilizationGauge cpuUtilizationGauge1m;
        private readonly CpuUtilizationGauge cpuUtilizationGauge5m;
        
        public MetricsManager()
        {
            cpuUtilizationTracker = new CpuUtilizationTracker(
                periodSeconds: 10.0,
                historySeconds: 5 * 60.0
            );
            
            cpuUsageGauge = new CpuUsageGauge(
                name: "worker_cpu_usage_seconds_total",
                help: "Cumulative system CPU time consumed in seconds"
            );
            
            cpuUtilizationGauge1m = new CpuUtilizationGauge(
                name: "worker_cpu_utilization",
                help: "Immediate CPU utilization in absolute vCPU units",
                cpuUtilizationTracker: cpuUtilizationTracker,
                timeWindow: 60.0
            ) {["window"] = "1m"};
            
            cpuUtilizationGauge5m = new CpuUtilizationGauge(
                name: "worker_cpu_utilization",
                help: "Immediate CPU utilization in absolute vCPU units",
                cpuUtilizationTracker: cpuUtilizationTracker,
                timeWindow: 5 * 60.0
            ) {["window"] = "5m"};
        }

        public void Dispose()
        {
            cpuUtilizationTracker.Dispose();
        }

        public string ToPrometheusTextFormat()
        {
            StringBuilder sb = new StringBuilder();

            cpuUsageGauge.ToPrometheusTextFormat(sb);
            cpuUtilizationGauge1m.ToPrometheusTextFormat(sb);
            cpuUtilizationGauge5m.ToPrometheusTextFormat(sb);

            return sb.ToString();
        }
    }
}