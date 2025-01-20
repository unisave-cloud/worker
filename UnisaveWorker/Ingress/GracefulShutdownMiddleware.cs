using System;
using System.Collections.Generic;
using System.Json;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace UnisaveWorker.Ingress
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
    
    /// <summary>
    /// Integrates the graceful shutdown manager with the HTTP OWIN stack
    /// </summary>
    public class GracefulShutdownMiddleware
    {
        private readonly AppFunc next;
        private readonly GracefulShutdownManager manager;

        public GracefulShutdownMiddleware(
            AppFunc next,
            GracefulShutdownManager manager
        )
        {
            this.next = next;
            this.manager = manager;
        }

        public async Task Invoke(IDictionary<string, object> environment)
        {
            bool canEnter = manager.OnRequestEnter();

            if (!canEnter)
            {
                await RespondWith503ShuttingDown(new OwinContext(environment));
                return;
            }
            
            try
            {
                await next(environment);
            }
            finally
            {
                manager.OnRequestExit();
            }
        }
        
        private async Task RespondWith503ShuttingDown(IOwinContext context)
        {
            var body = new JsonObject {
                ["error"] = true,
                ["code"] = 503,
                ["message"] = $"Worker is shutting down."
            };
            await context.SendResponse(
                statusCode: 503,
                body: body.ToString(),
                contentType: "application/json"
            );
        }
    }
}