using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnisaveWorker.Concurrency;

namespace WorkerTests
{
    [TestFixture]
    public class RequestConcurrencyMiddlewareTest
    {
        [Test]
        public async Task ItLimitsConcurrency()
        {
            // counters and their lock
            int currentConcurrency = 0;
            int highestConcurrency = 0;
            int finishedRequests = 0;
            object myLock = new object();
            
            // a barrier that halts requests until submitted
            var tcs = new TaskCompletionSource<int>();
            
            // a dummy request handler
            async Task MyAppFunc(IDictionary<string, object> env)
            {
                // enter
                lock (myLock)
                {
                    currentConcurrency++;
                    if (currentConcurrency > highestConcurrency)
                        highestConcurrency = currentConcurrency;
                }
                
                // just wait for the barrier to be submitted
                int barrierValue = await tcs.Task;
                Assert.AreEqual(42, barrierValue);
                
                // exit
                lock (myLock)
                {
                    currentConcurrency--;
                    finishedRequests++;
                }
            }

            // create the middleware we are about to test
            var middleware = new RequestConcurrencyMiddleware(
                next: MyAppFunc,
                maxConcurrency: 10
            );
            
            // submit 30 tasks into the pipeline
            List<Task> requestTasks = new List<Task>();
            for (int i = 0; i < 30; i++)
            {
                requestTasks.Add(
                    Task.Run(() => middleware.Invoke(null))
                );
            }
            
            // wait for the 10 tasks to hit the barrier
            while (currentConcurrency < 10)
                await Task.Yield();
            
            // wait some more
            await Task.Delay(100);
            
            // release the barrier
            tcs.SetResult(42);
            
            // wait for all requests to finish
            await Task.WhenAll(requestTasks.ToArray());
            
            // check that the concurrency never exceeded 10
            Assert.AreEqual(0, currentConcurrency);
            Assert.AreEqual(30, finishedRequests);
            Assert.AreEqual(10, highestConcurrency);
        }
    }
}