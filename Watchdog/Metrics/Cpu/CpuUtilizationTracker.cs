using System;
using System.Threading;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Watchdog.Metrics.Cpu
{
    public class CpuUtilizationTracker : IDisposable
    {
        private readonly double periodSeconds;

        /// <summary>
        /// List of previous measurements
        /// (cyclic buffer)
        /// </summary>
        private readonly double[] measurements;

        private readonly int bufferSize;

        private readonly Timer timer;

        /// <summary>
        /// Pointer into the measurements array at the next value to be set
        /// </summary>
        private int nextMeasurementIndex = 0;
        
        private readonly object syncLock = new object();
        
        public CpuUtilizationTracker(double periodSeconds, double historySeconds)
        {
            this.periodSeconds = periodSeconds;
            
            // allocate measurements
            // (buffer size +1 to allow for same window aggregation as the history)
            bufferSize = (int) Math.Ceiling(historySeconds / periodSeconds) + 1;
            measurements = new double[bufferSize];

            // initialize measurements
            double initialMeasurement = CpuUsageGauge.PerformMeasurement();
            for (int i = 0; i < measurements.Length; i++)
                measurements[i] = initialMeasurement;
            
            // setup timer
            timer = new Timer(periodSeconds);
            timer.AutoReset = true;
            timer.Elapsed += OnTimerTick;
            timer.Enabled = true;
        }

        private void OnTimerTick(object sender, ElapsedEventArgs e)
        {
            double measurement = CpuUsageGauge.PerformMeasurement();
            
            lock (syncLock)
            {
                measurements[nextMeasurementIndex] = measurement;
                
                nextMeasurementIndex = WrapBufferIndex(nextMeasurementIndex + 1);
            }
        }

        /// <summary>
        /// Computes current CPU utilization over a given time window
        /// </summary>
        public double ComputeUtilization(double windowSeconds)
        {
            int windowSamples = (int) Math.Ceiling(windowSeconds / periodSeconds);
            
            lock (syncLock)
            {
                int endIndex = WrapBufferIndex(nextMeasurementIndex - 1);
                int startIndex = WrapBufferIndex(endIndex - windowSamples);

                double cpuUsagePerWindow = measurements[endIndex] - measurements[startIndex];
                double cpuUsagePerSample = cpuUsagePerWindow / windowSamples;
                double cpuUsagePerSecond = cpuUsagePerSample / periodSeconds;

                // usage per second = used CPU seconds per second
                // = vCPU utilization units
                return cpuUsagePerSecond;
            }
        }

        private int WrapBufferIndex(int index)
        {
            index %= bufferSize;
            if (index < 0)
                index += bufferSize;
            return index;
        }
        
        public void Dispose()
        {
            timer.Dispose();
        }
    }
}