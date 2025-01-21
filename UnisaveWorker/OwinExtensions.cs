using System;
using System.IO;
using System.Json;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;

namespace UnisaveWorker
{
    /// <summary>
    /// Custom extension methods for OWIN
    /// </summary>
    public static class OwinExtensions
    {
        /// <summary>
        /// A helper method to define simple HTTP routes based on HTTP path
        /// </summary>
        /// <param name="appBuilder"></param>
        /// <param name="method">
        /// HTTP method to match, e.g. "GET" or "POST"
        /// </param>
        /// <param name="route">
        /// What path to match *exactly*, e.g. "/foo" or "/"
        /// </param>
        /// <param name="handler">
        /// Async function that accepts the OWIN context and handles the request
        /// </param>
        public static void Route(
            this IAppBuilder appBuilder,
            string method,
            string route,
            Func<IOwinContext, Task> handler
        )
        {
            appBuilder.MapWhen(
                ctx => ctx.Request.Method == method &&
                       ctx.Request.Path.Value == route,
                branch => branch.Run(handler)
            );
        }

        /// <summary>
        /// Sends a fixed-size HTTP response with an optional string body
        /// </summary>
        /// <param name="context">
        /// The OWIN context of the request
        /// </param>
        /// <param name="statusCode">
        /// HTTP status code, defaults to 200
        /// </param>
        /// <param name="body">
        /// Optional response body (will be UTF-8 encoded),
        /// null means no response body will be sent.
        /// </param>
        /// <param name="contentType">
        /// Content-Type header value, e.g. "text/plain" or "application/json".
        /// Defaults to plain text.
        /// </param>
        public static async Task SendResponse(
            this IOwinContext context,
            int statusCode = 200,
            string? body = null,
            string contentType = "text/plain"
        )
        {
            context.Response.ContentType = contentType;
            context.Response.StatusCode = statusCode;

            // send body-less response
            if (body == null)
            {
                context.Response.Body.Close();
                return;
            }
            
            // send response with body
            context.Response.Headers["Content-Length"]
                = body.Length.ToString();

            try
            {
                await context.Response.WriteAsync(body);
            }
            catch (IOException e)
                when (e.InnerException is ObjectDisposedException)
            {
                string requestPath = context.Request.Path.ToString();
                bool callCancelled = context.Request.CallCancelled
                    .IsCancellationRequested;
                
                Log.Warning(
                    $"Response for '{requestPath}' was not sent, " +
                    $"client probably closed the connection. " +
                    $"CallCancelled: {callCancelled}, Message: {e.Message}"
                );
            }
        }

        /// <summary>
        /// Formats a "Worker Error" HTTP response.
        /// See the documentation on "Error codes and meanings" to learn more.
        /// </summary>
        /// <param name="context">
        /// The OWIN context of the request
        /// </param>
        /// <param name="statusCode">
        /// HTTP Status code of the response
        /// </param>
        /// <param name="errorNumber">
        /// Worker-specific error number
        /// </param>
        /// <param name="errorMessage">
        /// Human-readable error message
        /// </param>
        public static async Task SendError(
            this IOwinContext context,
            int statusCode,
            int errorNumber,
            string errorMessage
        )
        {
            // headers
            context.Response.Headers.Set("X-Unisave-Worker-Error", "true");
            
            // body
            var body = new JsonObject {
                ["statusCode"] = statusCode,
                ["error"] = true,
                ["errorNumber"] = errorNumber,
                ["errorMessage"] = errorMessage
            };
            
            // send
            await context.SendResponse(
                statusCode: statusCode,
                body: body.ToString(),
                contentType: "application/json"
            );
        }
    }
}