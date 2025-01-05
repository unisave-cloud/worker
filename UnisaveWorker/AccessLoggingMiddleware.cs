using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace UnisaveWorker
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
    
    /// <summary>
    /// Assigns request index to unisave requests and when they are finished,
    /// it logs their presence and duration. It logs only if the request
    /// does not terminate in an uncaught exception.
    /// </summary>
    public class AccessLoggingMiddleware
    {
        private readonly AppFunc next;

        /// <summary>
        /// Tracks handled request index
        /// </summary>
        private int nextRequestIndex = 0;

        public AccessLoggingMiddleware(AppFunc next)
        {
            this.next = next;
        }
        
        public async Task Invoke(IDictionary<string, object> environment)
        {
            AssignRequestIndex(environment);
            
            var executionStopwatch = Stopwatch.StartNew();
            await next(environment);
            executionStopwatch.Stop();
            
            LogAccess(environment, executionStopwatch.ElapsedMilliseconds);
        }

        private void AssignRequestIndex(IDictionary<string, object> environment)
        {
            int requestIndex = Interlocked.Increment(ref nextRequestIndex) - 1;
            
            environment["worker.RequestIndex"] = requestIndex;
        }

        private void LogAccess(
            IDictionary<string, object> environment,
            long milliseconds
        )
        {
            // NOTE: This will log Unisave HTTP requests as they come from the
            // gateway, after the protocol between the gateway and the worker
            // is updated to handle any HTTP request.
            // Currently, it's an approximation of what it will look like.
            
            int requestIndex = environment["worker.RequestIndex"] as int? ?? -1;
            string facetClass = environment["worker.FacetClass"] as string;
            string facetMethod = environment["worker.FacetMethod"] as string;
            
            string id = "R" + requestIndex; // will be request ID sent via header
            string now = DateTime.UtcNow.ToString("yyyy-dd-MM H:mm:ss");
            
            var ctx = new OwinContext(environment);
            // string method = ctx.Request.Method;
            string method = "POST";
            // string path = ctx.Request.Path.Value;
            string path = $"/{facetClass}/{facetMethod}";
            
            string status = ctx.Response.StatusCode.ToString();
            string bytesSent = ctx.Response.Headers["Content-Length"] ?? "-";
            
            // [2023-12-03 21:52:37] R1385 POST /MyFacet/Foo 200 138B 45ms
            Console.WriteLine(
                $"[{now}] {id} {method} {path} {status} {bytesSent}B {milliseconds}ms"
            );
        }
    }
}