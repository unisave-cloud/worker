using System;
using System.Timers;
using UnisaveWorker.Metrics.Memory;

namespace UnisaveWorker.Health
{
    /// <summary>
    /// Monitors the total usage of memory and when it grows above a threshold
    /// it labels the worker as unhealthy to be restarted by kubernetes.
    /// </summary>
    public class MemoryLeakageMonitor : IDisposable
    {
        /// <summary>
        /// Is the memory leaking too high?
        /// </summary>
        public bool IsLeakingOverThreshold { get; private set; } = false;

        /// <summary>
        /// How often should the memory usage be checked
        /// </summary>
        private const double CheckIntervalSeconds = 60.0;
        
        /// <summary>
        /// When this memory usage amount is surpassed,
        /// the worker is considered unhealthy
        /// </summary>
        private readonly long memoryUsageThresholdBytes;

        private readonly Timer timer;
        
        public MemoryLeakageMonitor(long memoryUsageThresholdBytes)
        {
            this.memoryUsageThresholdBytes = memoryUsageThresholdBytes;
            
            timer = new Timer(CheckIntervalSeconds * 1000);
            timer.AutoReset = true;
            timer.Elapsed += OnTimerTick;
            timer.Start();
        }

        private void OnTimerTick(
            object sender,
            ElapsedEventArgs elapsedEventArgs
        )
        {
            bool oldState = IsLeakingOverThreshold;

            // update the state
            long currentUsage = (long) MemoryUsageGauge.GetMemoryUsageBytes();
            IsLeakingOverThreshold = (currentUsage > memoryUsageThresholdBytes);

            // log a change
            if (oldState != IsLeakingOverThreshold)
            {
                if (IsLeakingOverThreshold)
                    Log.Info("Memory leaking over threshold, becoming unhealthy.");
                else
                    Log.Info("Memory fell back below the threshold.");
            }
        }
        
        public void Dispose()
        {
            timer.Dispose();
        }
    }
}