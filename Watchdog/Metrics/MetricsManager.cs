using System;
using System.Text;
using Watchdog.Metrics.Cpu;
using Watchdog.Metrics.Other;

namespace Watchdog.Metrics
{
    public class MetricsManager : IDisposable
    {
        private readonly CpuUtilizationTracker cpuUtilizationTracker;
        
        private readonly CpuUsageCounter cpuUsageCounter;
        private readonly CpuUtilizationGauge cpuUtilizationGauge1M;
        private readonly CpuUtilizationGauge cpuUtilizationGauge5M;

        private readonly UptimeCounter uptimeCounter;
        
        public MetricsManager(Config config)
        {
            cpuUtilizationTracker = new CpuUtilizationTracker(
                periodSeconds: 10.0,
                historySeconds: 5 * 60.0
            );
            
            cpuUsageCounter = new CpuUsageCounter(
                name: "worker_cpu_usage_seconds_total",
                help: "Cumulative system CPU time consumed in seconds"
            ) {
                ["environment"] = config.WorkerEnvironmentId,
                ["backend"] = config.WorkerBackendId
            };
            
            cpuUtilizationGauge1M = new CpuUtilizationGauge(
                name: "worker_cpu_utilization",
                help: "Immediate CPU utilization in absolute vCPU units",
                cpuUtilizationTracker: cpuUtilizationTracker,
                timeWindow: 60.0
            ) {
                ["window"] = "1m",
                ["environment"] = config.WorkerEnvironmentId,
                ["backend"] = config.WorkerBackendId
            };
            
            cpuUtilizationGauge5M = new CpuUtilizationGauge(
                name: "worker_cpu_utilization",
                help: "Immediate CPU utilization in absolute vCPU units",
                cpuUtilizationTracker: cpuUtilizationTracker,
                timeWindow: 5 * 60.0
            ) {
                ["window"] = "5m",
                ["environment"] = config.WorkerEnvironmentId,
                ["backend"] = config.WorkerBackendId
            };
            
            uptimeCounter = new UptimeCounter(
                name: "worker_uptime_seconds",
                help: "Worker instance uptime seconds"
            ) {
                ["environment"] = config.WorkerEnvironmentId,
                ["backend"] = config.WorkerBackendId
            };
        }

        public void Dispose()
        {
            cpuUtilizationTracker.Dispose();
        }

        public string ToPrometheusTextFormat()
        {
            StringBuilder sb = new StringBuilder();

            cpuUsageCounter.ToPrometheusTextFormat(sb);
            cpuUtilizationGauge1M.ToPrometheusTextFormat(sb);
            cpuUtilizationGauge5M.ToPrometheusTextFormat(sb);
            
            uptimeCounter.ToPrometheusTextFormat(sb);

            return sb.ToString();
        }
    }
}