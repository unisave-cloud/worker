using System;
using System.IO;

namespace Watchdog
{
    /// <summary>
    /// Keeps track of whether the worker instance is healthy or not
    ///
    /// The worker starts healthy and then can only become unhealthy.
    /// Once it's unhealthy it has to be restarted.
    /// </summary>
    public class HealthStateManager : IDisposable
    {
        public const string LockFilePath = "/tmp/.unisave-worker-liveness-lock";

        /// <summary>
        /// Creates the lock file on application startup
        /// </summary>
        public void Initialize()
        {
            CreateLockFileIfMissing();
        }

        /// <summary>
        /// Removes the lock file on application exit
        /// </summary>
        public void Dispose()
        {
            RemoveLockFileIfExists();
        }

        /// <summary>
        /// Checks that the lock file is present
        /// </summary>
        public bool IsHealthy()
        {
            return File.Exists(LockFilePath);
        }

        /// <summary>
        /// Removes the lock file
        /// </summary>
        public void SetUnhealthy()
        {
            Log.Warning("Becoming unhealthy...");
            
            RemoveLockFileIfExists();
        }

        private void CreateLockFileIfMissing()
        {
            if (File.Exists(LockFilePath))
            {
                Log.Info("Creating lock file, but it's already present.");
                return;
            }

            File.Create(LockFilePath).Dispose();
            
            Log.Info("Lock file created.");
        }

        private void RemoveLockFileIfExists()
        {
            if (!File.Exists(LockFilePath))
            {
                Log.Info("Removing lock file, but it's already missing.");
                return;
            }

            File.Delete(LockFilePath);
            
            Log.Info("Lock file removed.");
        }
    }
}