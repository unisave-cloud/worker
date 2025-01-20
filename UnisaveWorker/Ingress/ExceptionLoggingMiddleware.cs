using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace UnisaveWorker.Ingress
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
    
    /// <summary>
    /// Catches uncaught request processing exception (from all routes),
    /// logs them to console and closes the request with 500 response.
    /// </summary>
    public class ExceptionLoggingMiddleware
    {
        private readonly AppFunc next;

        public ExceptionLoggingMiddleware(AppFunc next)
        {
            this.next = next;
        }
        
        [SuppressMessage(
            "Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "For diagnostics"
        )]
        public async Task Invoke(IDictionary<string, object> environment)
        {
            try
            {
                await next(environment);
            }
            catch (Exception ex)
            {
                try
                {
                    LogException(ex);
                    await RespondWith500(new OwinContext(environment));
                    return;
                }
                catch (Exception)
                {
                    // If there's an Exception while logging the error page,
                    // re-throw the original exception.
                }
                throw;
            }
        }
        
        private void LogException(Exception ex)
        {
            Log.Error("Unhandled worker exception: " + ex);
        }

        private async Task RespondWith500(IOwinContext context)
        {
            try
            {
                await context.SendError(
                    statusCode: 500,
                    errorNumber: 1,
                    $"Unhandled worker exception. See worker logs."
                );
            }
            catch
            {
                // This probably happens because we cannot send a response,
                // since the original exception happened in the middle of
                // a response being sent. So try 500 and just close the stream.
                context.Response.StatusCode = 500;
                context.Response.Body.Close();
            }
        }
    }
}