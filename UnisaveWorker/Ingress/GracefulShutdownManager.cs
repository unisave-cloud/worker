using System;
using System.Threading;

namespace UnisaveWorker.Ingress
{
    /// <summary>
    /// Controls the graceful shutdown period for the HTTP server
    /// </summary>
    public class GracefulShutdownManager
    {
        /// <summary>
        /// Is the shutdown being undertaken right now?
        /// </summary>
        private bool isShuttingDown = false;
        
        /// <summary>
        /// How many requests are currently inside the application,
        /// being processed
        /// </summary>
        private int pendingRequestCount = 0;
        
        /// <summary>
        /// Protects the bool and int above
        /// </summary>
        private readonly object syncLock = new object();
        
        /// <summary>
        /// Used when there are pending requests to signal the main thread
        /// to wake up when the last pending request finishes
        /// </summary>
        private readonly ManualResetEvent waitHandle
            = new ManualResetEvent(initialState: false);
        
        /// <summary>
        /// Called by the middleware when a request enters the server
        /// </summary>
        /// <returns>
        /// Returns true if the request should be handled, false if rejected
        /// </returns>
        public bool OnRequestEnter()
        {
            lock (syncLock)
            {
                if (isShuttingDown)
                    return false;
                
                pendingRequestCount++;
                return true;
            }
        }

        /// <summary>
        /// Called by the middleware when a request exits the server
        /// </summary>
        public void OnRequestExit()
        {
            lock (syncLock)
            {
                pendingRequestCount--;

                // the last request finished, wake up the main thread
                if (isShuttingDown && pendingRequestCount <= 0)
                    waitHandle.Set();
            }
        }
        
        /// <summary>
        /// Switches the middleware into rejecting new requests
        /// and waits for pending requests to finish
        /// </summary>
        /// <param name="timeout">
        /// How long to wait for pending requests at the longest
        /// </param>
        /// <returns>
        /// Returns true if the shutdown was successful, false if it timed out.
        /// </returns>
        public bool PerformGracefulShutdown(TimeSpan timeout)
        {
            lock (syncLock)
            {
                if (isShuttingDown)
                    throw new InvalidOperationException(
                        "Shutdown can only be called once"
                    );
                
                // start rejecting new requests
                isShuttingDown = true;
                
                // if there are no pending requests, stop right away
                if (pendingRequestCount <= 0)
                    return true;
            }
            
            Log.Info("Waiting for pending requests to finish...");
            
            // otherwise wait for pending requests (or the timeout) ot finish
            bool signalled = waitHandle.WaitOne(timeout);
            
            if (!signalled)
                Log.Warning("Stopping despite pending requests.");

            return signalled;
        }
    }
}