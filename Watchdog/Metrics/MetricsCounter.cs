namespace Watchdog.Metrics
{
    /// <summary>
    /// Counts a single value
    /// </summary>
    public class MetricsCounter : PrometheusMetric
    {
        public double Value { get; private set; }
        
        private readonly object syncLock = new object();

        protected MetricsCounter(string name, string help = null)
            : base(name, help, "counter") { }

        public void Increment(double amount)
        {
            lock (syncLock)
            {
                Value += amount;
            }
        }

        protected override double GetMetricValue()
        {
            // read can be without a lock since
            // x64 load and store instructions are atomic
            return Value;
        }
    }
}