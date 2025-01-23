using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace UnisaveWorker.Concurrency.Loop
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
    
    /// <summary>
    /// Executes downstream OWIN pipeline in a single thread, allowing for
    /// single-threaded asynchronous concurrency
    /// </summary>
    public class LoopMiddleware
    {
        private readonly AppFunc next;
        
        private readonly TaskFactory taskFactory;

        public LoopMiddleware(
            AppFunc next,
            LoopScheduler scheduler
        )
        {
            this.next = next;
            
            taskFactory = new TaskFactory(scheduler);
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