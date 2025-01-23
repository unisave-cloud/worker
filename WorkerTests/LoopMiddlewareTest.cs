using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnisaveWorker.Concurrency.Loop;

namespace WorkerTests
{
    [TestFixture]
    public class LoopMiddlewareTest
    {
        // OWIN environment
        private readonly Dictionary<string, object> dummyRequest
            = new Dictionary<string, object>();
        
        // the scheduler that's actually being tested
        private LoopScheduler scheduler;
        
        [SetUp]
        public void SetUp()
        {
            scheduler = new LoopScheduler();
        }

        [TearDown]
        public void TearDown()
        {
            scheduler.Dispose();
        }
        
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
            var middleware = new LoopMiddleware(
                next: MyAppFunc,
                scheduler: scheduler
            );
            
            // submit 30 tasks into the pipeline
            List<Task> requestTasks = new List<Task>();
            for (int i = 0; i < 30; i++)
            {
                requestTasks.Add(
                    Task.Run(() => middleware.Invoke(dummyRequest))
                );
            }
            
            // wait for the first tasks to hit the barrier
            while (currentConcurrency < 1)
                Thread.Yield();
            
            // wait some more
            Thread.Sleep(100);
            
            // release the barrier
            waitHandle.Set();
            
            // wait for all requests to finish
            await Task.WhenAll(requestTasks.ToArray());
            
            // check that the concurrency never exceeded 1
            Assert.AreEqual(0, currentConcurrency);
            Assert.AreEqual(30, finishedRequests);
            Assert.AreEqual(1, highestConcurrency);
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
            var tcs = new TaskCompletionSource<bool>();
            
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
            var middleware = new LoopMiddleware(
                next: MyAppFunc,
                scheduler: scheduler
            );
            
            // submit 30 tasks into the pipeline
            List<Task> requestTasks = new List<Task>();
            for (int i = 0; i < 30; i++)
            {
                requestTasks.Add(
                    Task.Run(() => middleware.Invoke(dummyRequest))
                );
            }
            
            // wait for all the tasks to hit the barrier
            while (currentAsyncConcurrency < requestTasks.Count)
                await Task.Yield();
            
            // release the barrier
            tcs.SetResult(true);
            
            // wait for all requests to finish
            await Task.WhenAll(requestTasks.ToArray());
            
            // check that the concurrency reached 30
            Assert.AreEqual(0, currentAsyncConcurrency);
            Assert.AreEqual(30, finishedRequests);
            Assert.AreEqual(30, highestAsyncConcurrency);
        }

        [Test]
        public async Task ItRecoversFromDeadlocks()
        {
            // NOTE: this test fails by never finishing (deadlocking)
            // if the deadlock recovery code is not present
            
            int finishedRequests = 0;
            
            async Task FakeIoOperation() // short, 50ms task
            {
                // the async-await wrapping is needed to trigger the deadlock
                // (I guess due to some inlining optimizations)
                await Task.Delay(TimeSpan.FromMilliseconds(50));
            }

            var deadlockRequest = new Dictionary<string, object> {
                ["deadlock"] = true
            };
            
            async Task MyAppFunc(IDictionary<string, object> env)
            {
                // now we run on the scheduler
                Assert.AreSame(scheduler, TaskScheduler.Current);
                
                // do nothing if we don't want to get deadlocked
                if (!env.ContainsKey("deadlock"))
                    return;
                
                // Create a deadlock
                // -----------------
                // Wait for a task synchronously, which:
                // 1. puts the child task onto the same scheduler as us
                // 2. makes us synchronously sleep until that task finishes
                // -> deadlock, waiting for ourselves
                FakeIoOperation().GetAwaiter().GetResult();
                
                // this could only be fixed by running the task on a different
                // scheduler (or by doing proper await), like this:
                // Task.Run(FakeIoOperation).GetAwaiter().GetResult(); // sync
                // await FakeIoOperation(); // async
                // await Task.Run(FakeIoOperation); // weird unnecessary combo
                
                // count finished requests
                Interlocked.Increment(ref finishedRequests);
            }
            
            // create the middleware we are about to test
            var middleware = new LoopMiddleware(
                next: MyAppFunc,
                scheduler: scheduler
            );

            // send two requests through the middleware
            // first will deadlock and second will wait
            // and be processed after the recovery
            Task firstRequest = Task.Run(() => middleware.Invoke(deadlockRequest));
            Task secondRequest = Task.Run(() => middleware.Invoke(dummyRequest));
            
            // both requests should have finished fine
            await firstRequest;
            await secondRequest;
            Assert.AreEqual(2, finishedRequests);
        }
    }
}