using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;

namespace UnisaveWorker
{
    public static class OwinExtensions
    {
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

        public static async Task SendResponse(
            this IOwinContext context,
            int statusCode = 200,
            string? body = null,
            string contentType = "text/plain"
        )
        {
            context.Response.ContentType = contentType;
            context.Response.StatusCode = statusCode;

            if (body == null)
            {
                context.Response.Body.Close();
            }
            else
            {
                context.Response.Headers["Content-Length"]
                    = body.Length.ToString();
                await context.Response.WriteAsync(body);
            }
        }
    }
}