using System;

namespace UnisaveWorker.Metrics.Memory
{
    public class GcMemoryGauge : PrometheusMetric
    {
        public GcMemoryGauge(string name, string help)
            : base(name, help, "gauge") { }

        protected override double GetMetricValue()
        {
            long bytes = GC.GetTotalMemory(false);
            return (double) bytes;
        }
    }
}