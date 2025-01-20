using System;
using System.Text;
using UnisaveWorker.Metrics.Cpu;
using UnisaveWorker.Metrics.Memory;
using UnisaveWorker.Metrics.Network;
using UnisaveWorker.Metrics.Other;

namespace UnisaveWorker.Metrics
{
    public class MetricsManager : IDisposable
    {
        private readonly CpuUtilizationTracker cpuUtilizationTracker;
        
        private readonly CpuUsageCounter cpuUsageCounter;
        private readonly CpuUtilizationGauge cpuUtilizationGauge1M;
        private readonly CpuUtilizationGauge cpuUtilizationGauge5M;

        private readonly MemoryUsageGauge memoryUsageGauge;
        private readonly GcMemoryGauge gcMemoryGauge;

        private readonly NetstatGauge networkRxGauge;
        private readonly NetstatGauge networkTxGauge;

        private readonly MetricsCounter requestCounter;
        private readonly MetricsCounter requestDurationCounter;
        private readonly MetricsCounter requestResponseSizeCounter;

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
            
            memoryUsageGauge = new MemoryUsageGauge(
                name: "worker_memory_usage_bytes",
                help: "Current memory usage in bytes, including all " +
                      "memory regardless of when it was accessed"
            ) {
                ["environment"] = config.WorkerEnvironmentId,
                ["backend"] = config.WorkerBackendId
            };
            
            gcMemoryGauge = new GcMemoryGauge(
                name: "worker_gc_memory_usage_bytes",
                help: "Managed memory usage in bytes, according to the GC class"
            ) {
                ["environment"] = config.WorkerEnvironmentId,
                ["backend"] = config.WorkerBackendId
            };
            
            networkRxGauge = new NetstatGauge(
                name: "worker_network_rx_bytes_total",
                help: "Total bytes received via the IP protocol",
                netstatGroup: "IpExt",
                netstatValue: "InOctets"
            ) {
                ["environment"] = config.WorkerEnvironmentId,
                ["backend"] = config.WorkerBackendId
            };
            
            networkTxGauge = new NetstatGauge(
                name: "worker_network_tx_bytes_total",
                help: "Total bytes transmitted via the IP protocol",
                netstatGroup: "IpExt",
                netstatValue: "OutOctets"
            ) {
                ["environment"] = config.WorkerEnvironmentId,
                ["backend"] = config.WorkerBackendId
            };
            
            requestCounter = new MetricsCounter(
                name: "worker_requests_total",
                help: "Total number of performed execution requests"
            ) {
                ["environment"] = config.WorkerEnvironmentId,
                ["backend"] = config.WorkerBackendId
            };
            
            requestDurationCounter = new MetricsCounter(
                name: "worker_request_duration_seconds_total",
                help: "Total number of seconds spent running execution requests"
            ) {
                ["environment"] = config.WorkerEnvironmentId,
                ["backend"] = config.WorkerBackendId
            };
            
            requestResponseSizeCounter = new MetricsCounter(
                name: "worker_request_response_bytes_total",
                help: "Size of all execution responses in bytes"
            ) {
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
            sb.AppendLine();
            
            memoryUsageGauge.ToPrometheusTextFormat(sb);
            gcMemoryGauge.ToPrometheusTextFormat(sb);
            sb.AppendLine();
            
            networkRxGauge.ToPrometheusTextFormat(sb);
            networkTxGauge.ToPrometheusTextFormat(sb);
            sb.AppendLine();
            
            requestCounter.ToPrometheusTextFormat(sb);
            requestDurationCounter.ToPrometheusTextFormat(sb);
            requestResponseSizeCounter.ToPrometheusTextFormat(sb);
            sb.AppendLine();
            
            uptimeCounter.ToPrometheusTextFormat(sb);
            sb.AppendLine();

            return sb.ToString();
        }

        public void RecordExecutionRequestFinished(
            double durationSeconds,
            double responseSizeBytes
        )
        {
            requestCounter.Increment(1.0);
            requestDurationCounter.Increment(durationSeconds);
            requestResponseSizeCounter.Increment(responseSizeBytes);
        }
    }
}