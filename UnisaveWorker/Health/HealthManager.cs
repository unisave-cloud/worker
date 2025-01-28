using System;

namespace UnisaveWorker.Health
{
    /// <summary>
    /// Monitors worker health
    /// </summary>
    public class HealthManager : IDisposable
    {
        private readonly MemoryLeakageMonitor memoryLeakageMonitor;

        public HealthManager(Config config)
        {
            memoryLeakageMonitor = new MemoryLeakageMonitor(
                config.UnhealthyMemoryUsageBytes
            );
        }

        /// <summary>
        /// Returns true if the worker is considered healthy
        /// </summary>
        public bool GetIsHealthy()
        {
            if (memoryLeakageMonitor.IsLeakingOverThreshold)
                return false;
            
            return true;
        }
        
        public void Dispose()
        {
            memoryLeakageMonitor.Dispose();
        }
    }
}