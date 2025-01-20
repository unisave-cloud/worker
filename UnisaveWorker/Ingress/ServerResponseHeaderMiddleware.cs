using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace UnisaveWorker.Ingress
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
    
    /// <summary>
    /// Sets the 'Server' response header for the worker,
    /// it does so when the request is first received, so subsequent
    /// middlewares can override the value.
    /// </summary>
    public class ServerResponseHeaderMiddleware
    {
        private readonly AppFunc next;

        private readonly string headerValue;

        public ServerResponseHeaderMiddleware(AppFunc next)
        {
            this.next = next;
            
            string version = typeof(ServerResponseHeaderMiddleware).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                .InformationalVersion;

            headerValue = $"UnisaveWorker/" + version;
        }

        public async Task Invoke(IDictionary<string, object> environment)
        {
            var responseHeaders = (IDictionary<string, string[]>)environment[
                "owin.ResponseHeaders"
            ];
            responseHeaders["Server"] = new string[] { headerValue };
            
            await next(environment);
        }
    }
}