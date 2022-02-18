using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Watchdog.Execution;

namespace Watchdog.Http
{
    public class Router
    {
        private readonly HealthStateManager healthStateManager;
        private readonly RequestQueue requestQueue;

        public Router(HealthStateManager healthStateManager, RequestQueue requestQueue)
        {
            this.healthStateManager = healthStateManager;
            this.requestQueue = requestQueue;
        }

        /// <summary>
        /// Entrypoint into the router
        /// </summary>
        /// <param name="context">The request context</param>
        /// <exception cref="ArgumentNullException"></exception>
        public Task HandleRequestAsync(HttpListenerContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // [execution request] (GET or POST)
            if (context.Request.Url.AbsolutePath == "/")
            {
                requestQueue.EnqueueRequest(context);
                return Task.CompletedTask;
            }
            
            // [worker status]
            if (context.Request.HttpMethod == "GET"
                && context.Request.Url.AbsolutePath == "/status")
            {
                // e.g. memory usage, queue size, etc...
                context.Response.StatusCode = 200;
                StringResponse(context, "TODO: status\n");
                return Task.CompletedTask;
            }
            
            // [health check]
            if (context.Request.HttpMethod == "GET"
                && context.Request.Url.AbsolutePath == "/_/health")
            {
                HealthCheckRequest(context);
                return Task.CompletedTask;
            }
            
            // [custom metrics]
            if (context.Request.HttpMethod == "GET"
                && context.Request.Url.AbsolutePath == "/metrics")
            {
                context.Response.StatusCode = 200;
                StringResponse(context, "TODO: metrics\n");
                return Task.CompletedTask;
            }
            
            // [default]
            context.Response.StatusCode = 404;
            StringResponse(context, "404 - Page not found.\n");
            return Task.CompletedTask;
        }

        private void HealthCheckRequest(HttpListenerContext context)
        {
            if (healthStateManager.IsHealthy())
            {
                context.Response.StatusCode = 200;
                StringResponse(context, "OK\n");
            }
            else
            {
                context.Response.StatusCode = 503;
                StringResponse(context, "Service Unavailable\n");
            }
        }
        
        /// <summary>
        /// Sends a string response encoded into UTF-8
        /// </summary>
        /// <param name="context">Request context</param>
        /// <param name="response">The actual string to send</param>
        /// <param name="contentType">What content type to present it as</param>
        public static void StringResponse(
            HttpListenerContext context,
            string response,
            string contentType = "text/plain"
        )
        {
            if (response == null)
                response = "null";
            
            try
            {
                byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                context.Response.Headers.Add("Content-Type", contentType);
                context.Response.ContentLength64 = responseBytes.Length;
                context.Response.OutputStream.Write(
                    responseBytes,
                    0,
                    responseBytes.Length
                );
            }
            finally
            {
                context.Response.OutputStream.Close();
            }
        }
    }
}