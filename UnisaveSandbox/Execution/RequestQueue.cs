using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace UnisaveSandbox.Execution
{
    /// <summary>
    /// Queues incoming execution requests that are not yet being executed
    /// </summary>
    public class RequestQueue
    {
        // TODO: limit queue length and then respond with 429 - too many reqs
        
        private readonly object queueLock = new object();
        
        private readonly Queue<HttpListenerContext> queue
            = new Queue<HttpListenerContext>();
        
        public void EnqueueRequest(HttpListenerContext context)
        {
            lock (queueLock)
            {
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
    }
}