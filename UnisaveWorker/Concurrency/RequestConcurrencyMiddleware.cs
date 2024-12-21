using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnisaveWorker.Concurrency
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
    
    /// <summary>
    /// Limits the number of concurrently handled requests.
    /// Over-limit requests stay hanging inside of this middleware until the
    /// in-flight requests finish. Acts basically like a semaphore.
    /// </summary>
    public class RequestConcurrencyMiddleware
    {
        private readonly AppFunc next;
        
        /// <summary>
        /// Maximum number of in-flight requests
        /// </summary>
        private readonly int maxConcurrency;
        
        /// <summary>
        /// How many requests are currently admitted
        /// </summary>
        private int currentConcurrency = 0;
        
        /// <summary>
        /// One TCS for each waiting request. Completing the TCS wakes up
        /// that waiting request and the request then assumes it has gotten
        /// the permission to run.
        /// </summary>
        private readonly Queue<TaskCompletionSource<object>> waitingRequests
            = new Queue<TaskCompletionSource<object>>();
        
        /// <summary>
        /// Lock for access to the shared state
        /// </summary>
        private readonly object syncLock = new object();

        /// <summary>
        /// Constructs a new instance of the middleware
        /// </summary>
        /// <param name="next">The request handler to call next</param>
        /// <param name="maxConcurrency">
        /// Maximum number of allowed concurrent requests
        /// through this middleware
        /// </param>
        public RequestConcurrencyMiddleware(AppFunc next, int maxConcurrency)
        {
            if (maxConcurrency < 1)
                throw new ArgumentOutOfRangeException(nameof(maxConcurrency));
            
            this.next = next;
            this.maxConcurrency = maxConcurrency;
        }

        public async Task Invoke(IDictionary<string, object> environment)
        {
            await AcquirePermissionToRun();

            try
            {
                // process the request
                await next(environment);
            }
            finally
            {
                ReleasePermissionToRun();
            }
        }

        private async Task AcquirePermissionToRun()
        {
            TaskCompletionSource<object> tcs;
            lock (syncLock)
            {
                // try just immediately running
                if (currentConcurrency < maxConcurrency)
                {
                    currentConcurrency++;
                    return;
                }
                
                // else enter the queue
                tcs = new TaskCompletionSource<object>();
                waitingRequests.Enqueue(tcs);
            }
            
            // we have to wait for our TCS to be completed
            // by some other request finishing and giving us their permission
            await tcs.Task;
        }

        private void ReleasePermissionToRun()
        {
            TaskCompletionSource<object> tcs;
            lock (syncLock)
            {
                // if there are waiting requests, and we are not running over
                // capacity, we give our permission to some waiting request
                if (waitingRequests.Count > 0
                    && currentConcurrency <= maxConcurrency)
                {
                    tcs = waitingRequests.Dequeue();
                }

                // else we destroy our permission
                else
                {
                    currentConcurrency--;
                    return;
                }
            }
            
            // wake up the waiting request
            tcs.SetResult(null);
        }
    }
}