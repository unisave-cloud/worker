using System;
using System.Collections.Generic;
using System.Json;
using System.Threading.Tasks;
using Microsoft.Owin;
using Watchdog;

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
        /// Maximum number of waiting requests
        /// </summary>
        private readonly int maxQueueLength;
        
        /// <summary>
        /// One TCS for each waiting request. Completing the TCS wakes up
        /// that waiting request and the request then assumes it has gotten
        /// the permission to run.
        /// </summary>
        private readonly Queue<TaskCompletionSource<object>> waitingRequests
            = new Queue<TaskCompletionSource<object>>();

        /// <summary>
        /// This exception is thrown internally when the request queue gets full
        /// </summary>
        private class QueueIsFullException : Exception { }
        
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
        /// <param name="maxQueueLength">
        /// Maximum number of requests waiting to be executed,
        /// when queue fills up, it generates 429 responses
        /// </param>
        public RequestConcurrencyMiddleware(
            AppFunc next,
            int maxConcurrency,
            int maxQueueLength
        )
        {
            if (maxConcurrency < 1)
                throw new ArgumentOutOfRangeException(nameof(maxConcurrency));
            
            this.next = next;
            this.maxConcurrency = maxConcurrency;
            this.maxQueueLength = maxQueueLength;
        }

        public async Task Invoke(IDictionary<string, object> environment)
        {
            try
            {
                await AcquirePermissionToRun();
            }
            catch (QueueIsFullException)
            {
                await RespondWith429QueueIsFull(environment);
                return;
            }

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
                
                // we cannot acquire the permission if the queue is full
                if (waitingRequests.Count >= maxQueueLength)
                    throw new QueueIsFullException();
                
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

        private async Task RespondWith429QueueIsFull(
            IDictionary<string, object> environment
        )
        {
            var ctx = new OwinContext(environment);
            
            // send the 429 response
            var body = new JsonObject {
                ["error"] = true,
                ["code"] = 429,
                ["message"] = $"Worker queue in the " +
                              $"{nameof(RequestConcurrencyMiddleware)} is full."
            };
            await ctx.SendResponse(
                statusCode: 429,
                body: body.ToString(),
                contentType: "application/json"
            );
            
            // log a warning
            environment.TryGetValue(
                "worker.RequestIndex",
                out object requestIndex
            );
            Log.Warning(
                $"Request {requestIndex} bounced due to " +
                $"{nameof(RequestConcurrencyMiddleware)} queue being full."
            );
        }
    }
}