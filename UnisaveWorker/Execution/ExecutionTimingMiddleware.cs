using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace UnisaveWorker.Execution
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
    
    /// <summary>
    /// Computes the time it takes to execute a request and assigns the value
    /// into the `worker.ExecutionDurationSeconds` OWIN environment dictionary
    /// </summary>
    public class ExecutionTimingMiddleware
    {
        private readonly AppFunc next;

        public ExecutionTimingMiddleware(AppFunc next)
        {
            this.next = next;
        }

        public async Task Invoke(IDictionary<string, object> environment)
        {
            // time the execution
            var stopwatch = Stopwatch.StartNew();
            await next(environment);
            stopwatch.Stop();
            
            // store the time in the request metadata
            environment["worker.ExecutionDurationSeconds"]
                = stopwatch.ElapsedMilliseconds / 1000.0;
        }
    }
}