using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Watchdog.Http;

namespace Watchdog.Execution
{
    /// <summary>
    /// Queues incoming execution requests that are not yet being executed
    /// </summary>
    public class RequestQueue : IDisposable
    {
        private readonly HealthStateManager healthStateManager;
        private readonly int maxQueueLength;

        private bool disposed = false;
        
        private readonly object queueLock = new object();
        
        private readonly Queue<HttpListenerContext> queue
            = new Queue<HttpListenerContext>();

        public RequestQueue(HealthStateManager healthStateManager, int maxQueueLength)
        {
            this.healthStateManager = healthStateManager;
            this.maxQueueLength = maxQueueLength;
        }

        public void Dispose()
        {
            lock (queueLock)
            {
                // already disposed
                if (disposed)
                    return;
                
                // reject any future requests
                disposed = true;
                
                // reject all waiting requests
                foreach (HttpListenerContext context in queue)
                    RejectRequestBecauseWorkerIsStopping(context);
                
                queue.Clear();
            }
        }

        public void EnqueueRequest(HttpListenerContext context)
        {
            // unhealthy worker
            if (!healthStateManager.IsHealthy())
            {
                RejectRequestBecauseWorkerUnhealthy(context);
                return;
            }
            
            // handle queuing
            lock (queueLock)
            {
                // stopping worker
                if (disposed)
                {
                    RejectRequestBecauseWorkerIsStopping(context);
                    return;
                }
                
                // queue full
                if (queue.Count >= maxQueueLength)
                {
                    RejectRequestBecauseQueueIsFull(context);
                    return;
                }
                
                // enqueue
                queue.Enqueue(context);
                Monitor.Pulse(queueLock);
            }
        }

        /// <summary>
        /// Blocks and waits for a request to be consumed.
        /// If cancelled, throws OperationCanceledException
        /// </summary>
        /// <returns></returns>
        public HttpListenerContext DequeueRequest(
            CancellationToken cancellationToken
        )
        {
            lock (queueLock)
            {
                while (queue.Count == 0)
                {
                    Monitor.Wait(queueLock);
                    
                    // handle cancellation
                    cancellationToken.ThrowIfCancellationRequested();
                }

                return queue.Dequeue();
            }
        }

        /// <summary>
        /// Pulses all waiting threads
        /// (needed to notice cancellation)
        /// </summary>
        public void PulseAll()
        {
            lock (queueLock)
            {
                Monitor.PulseAll(queueLock);
            }
        }

        private void RejectRequestBecauseWorkerUnhealthy(HttpListenerContext context)
        {
            // EXPLANATION:
            // Worker is unhealthy and so it won't serve requests.
            // Wait for the worker to restart or send the request
            // to a different worker instead.
            
            context.Response.StatusCode = 500;
            Router.StringResponse(context, "Worker is unhealthy\n");
            
            Log.Warning("Rejected request due to being unhealthy.");
        }

        private void RejectRequestBecauseQueueIsFull(HttpListenerContext context)
        {
            // EXPLANATION:
            // Worker queue is full and so any further accepted requests
            // would wait too long or they would overwhelm the worker.
            // Send this request to another worker or wait for a while.
            
            context.Response.StatusCode = 429;
            Router.StringResponse(context, "Worker queue is full\n");
            
            Log.Warning("Rejected request due to queue being full.");
        }

        private void RejectRequestBecauseWorkerIsStopping(HttpListenerContext context)
        {
            // EXPLANATION:
            // Worker is being stopped and so no more requests can be served.
            // Send the request to a different worker.
            
            context.Response.StatusCode = 500;
            Router.StringResponse(context, "Worker is stopping\n");
            
            Log.Warning("Rejected request due to stopping.");
        }
    }
}