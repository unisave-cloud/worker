using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Owin;
using Watchdog;

namespace UnisaveWorker
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
    
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
                    RespondWith500(new OwinContext(environment));
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

        private void RespondWith500(IOwinContext context)
        {
            context.Response.StatusCode = 500;
            context.Response.Body.Close();
        }
    }
}