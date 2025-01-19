using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace UnisaveWorker.Concurrency
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
    
    /// <summary>
    /// Limits the number of concurrently running threads processing requests
    /// behind this middleware.
    ///
    /// The solution is based on this StackOverflow post:
    /// https://stackoverflow.com/questions/69222176/run-valuetasks-on-a-custom-thread-pool
    ///
    /// Which uses the code sample shown here at MSDN:
    /// https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.taskscheduler
    /// </summary>
    public class ThreadConcurrencyMiddleware
    {
        private readonly AppFunc next;

        private TaskFactory taskFactory = null!;
        
        /// <summary>
        /// Constructs a new instance of the middleware
        /// </summary>
        /// <param name="next">The request handler to call next</param>
        /// <param name="maxConcurrency">
        /// Maximum number of allowed concurrent threads
        /// behind this middleware
        /// </param>
        public ThreadConcurrencyMiddleware(AppFunc next, int maxConcurrency)
        {
            this.next = next;
            
            // creates and sets the task factory field
            SetMaxConcurrency(maxConcurrency);
        }

        /// <summary>
        /// Change the thread concurrency level for future requests
        /// </summary>
        public void SetMaxConcurrency(int maxConcurrency)
        {
            if (maxConcurrency < 1)
                throw new ArgumentOutOfRangeException(nameof(maxConcurrency));
            
            taskFactory = new TaskFactory(
                new LimitedConcurrencyLevelTaskScheduler(
                    maxConcurrency
                )
            );
        }
        
        public async Task Invoke(IDictionary<string, object> environment)
        {
            // Roughly this, but with the custom scheduler:
            // await Task.Run(() => next(environment));
            
            var context = new OwinContext(environment);
            
            // Gets translated to this, when using a task factory:
            Task<Task> wrappedTask = taskFactory.StartNew(
                () => next(environment),
                context.Request.CallCancelled
            );
            Task requestTask = await wrappedTask;
            await requestTask;
            
            // The wrappedTask is what the task factory creates. It gets run
            // by the scheduler at some point, and then when it does,
            // it returns the internal next(environment) task, which has had
            // the proper scheduler set and that gets run by awaiting it again.
            //
            // It's weird, I know. Try disassembling Task.Run and you'll
            // find an internal UnwrapPromise, which does the same thing.
        }
    }
}