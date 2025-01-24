using System;
using System.Timers;

namespace UnisaveWorker.Concurrency.Loop
{
    /// <summary>
    /// Service that accompanies a loop thread and detects deadlocks
    /// (or rather timeouts) with task executions. When detected,
    /// it signals back to the scheduler to replace the loop thread.
    /// </summary>
    public class DeadlockObserver
    {
        private Timer? timer;
        
        public DeadlockObserver(
            double timeoutSeconds,
            Action<DeadlockObserver> reportDeadlockHandler
        )
        {
            timer = new Timer(timeoutSeconds * 1_000);
            timer.AutoReset = false;
            
            timer.Elapsed += (sender, args) =>
            {
                // prevent the timer from being re-scheduled after it fired
                timer = null;
                
                // fire the timeout event
                reportDeadlockHandler.Invoke(this);
            };
        }
        
        public void StartTimer()
        {
            timer?.Start();
        }

        public void StopTimer()
        {
            timer?.Stop();
        }
        
        public void ResetTimer()
        {
            // reset
            timer?.Stop();
            timer?.Start();
        }
    }
}