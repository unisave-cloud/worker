using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnisaveWorker.Concurrency;

namespace WorkerTests
{
    [TestFixture]
    public class ThreadConcurrencyMiddlewareTest
    {
        [Test]
        public async Task ItLimitsConcurrency()
        {
            // counters and their lock
            int currentConcurrency = 0;
            int highestConcurrency = 0;
            int finishedRequests = 0;
            object myLock = new object();
            
            // a barrier that halts requests until signalled
            var waitHandle = new ManualResetEvent(initialState: false);
            
            Task MyAppFunc(IDictionary<string, object> env)
            {
                // enter
                lock (myLock)
                {
                    currentConcurrency++;
                    if (currentConcurrency > highestConcurrency)
                        highestConcurrency = currentConcurrency;
                }
                
                // just wait for the barrier to be submitted
                waitHandle.WaitOne();
                
                // exit
                lock (myLock)
                {
                    currentConcurrency--;
                    finishedRequests++;
                }

                return Task.CompletedTask;
            }

            // create the middleware we are about to test
            var middleware = new ThreadConcurrencyMiddleware(
                next: MyAppFunc,
                maxConcurrency: 2
            );
            
            // submit 30 tasks into the pipeline
            List<Task> requestTasks = new List<Task>();
            for (int i = 0; i < 30; i++)
            {
                requestTasks.Add(
                    Task.Run(() => middleware.Invoke(null))
                );
            }
            
            // wait for the 2 tasks to hit the barrier
            while (currentConcurrency < 2)
                Thread.Yield();
            
            // wait some more
            Thread.Sleep(100);
            
            // release the barrier
            waitHandle.Set();
            
            // wait for all requests to finish
            await Task.WhenAll(requestTasks.ToArray());
            
            // check that the concurrency never exceeded 2
            Assert.AreEqual(0, currentConcurrency);
            Assert.AreEqual(30, finishedRequests);
            Assert.AreEqual(2, highestConcurrency);
        }

        [Test]
        public async Task ItRunsAsynchronousCodeConcurrently()
        {
            // counters and their lock
            int currentAsyncConcurrency = 0;
            int highestAsyncConcurrency = 0;
            int finishedRequests = 0;
            object myLock = new object();
            
            // a barrier that halts requests until signalled
            var tcs = new TaskCompletionSource<object>();
            
            async Task MyAppFunc(IDictionary<string, object> env)
            {
                // enter
                lock (myLock)
                {
                    currentAsyncConcurrency++;
                    if (currentAsyncConcurrency > highestAsyncConcurrency)
                        highestAsyncConcurrency = currentAsyncConcurrency;
                }
                
                // just wait for the barrier to be submitted
                await tcs.Task;
                
                // exit
                lock (myLock)
                {
                    currentAsyncConcurrency--;
                    finishedRequests++;
                }
            }
            
            // create the middleware we are about to test
            var middleware = new ThreadConcurrencyMiddleware(
                next: MyAppFunc,
                maxConcurrency: 1 // just one thread
            );
            
            // submit 30 tasks into the pipeline
            List<Task> requestTasks = new List<Task>();
            for (int i = 0; i < 30; i++)
            {
                requestTasks.Add(
                    Task.Run(() => middleware.Invoke(null))
                );
            }
            
            // wait for all of the tasks to hit the barrier
            while (currentAsyncConcurrency < requestTasks.Count)
                await Task.Yield();
            
            // release the barrier
            tcs.SetResult(null);
            
            // wait for all requests to finish
            await Task.WhenAll(requestTasks.ToArray());
            
            // check that the concurrency never exceeded 2
            Assert.AreEqual(0, currentAsyncConcurrency);
            Assert.AreEqual(30, finishedRequests);
            Assert.AreEqual(30, highestAsyncConcurrency);
        }
    }
}