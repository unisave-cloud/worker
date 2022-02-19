namespace Watchdog.Metrics.Cpu
{
    public class CpuUtilizationGauge : PrometheusMetric
    {
        private readonly CpuUtilizationTracker cpuUtilizationTracker;
        private readonly double timeWindow;
        
        public CpuUtilizationGauge(
            string name,
            string help,
            CpuUtilizationTracker cpuUtilizationTracker,
            double timeWindow
        ) : base(name, help, "gauge")
        {
            this.cpuUtilizationTracker = cpuUtilizationTracker;
            this.timeWindow = timeWindow;
        }
        
        protected override double GetMetricValue()
        {
            return cpuUtilizationTracker.ComputeUtilization(timeWindow);
        }
    }
}