using System;
using System.Collections.Generic;
using System.Json;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace UnisaveWorker.Initialization
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
    
    /// <summary>
    /// Triggers lazy initialization and ensures worker is initialized
    /// before letting requests pass through.
    /// </summary>
    public class InitializationMiddleware
    {
        private readonly AppFunc next;
        private readonly Initializer initializer;
        
        public InitializationMiddleware(AppFunc next, Initializer initializer)
        {
            this.next = next;
            this.initializer = initializer;
        }

        public async Task Invoke(IDictionary<string, object> environment)
        {
            var context = new OwinContext(environment);
            string? recipeUrl = context.Request.Headers[
                "X-Unisave-Initialization-Recipe-Url"
            ];
            
            // make sure that the initialization recipe URL is provided
            // if we need initialization
            if (initializer.State == InitializationState.NonInitialized
                && recipeUrl == null)
            {
                await RespondWith409MissingRecipeUrl(context);
                return;
            }
            
            // start lazy initialization
            // (does nothing if already initialized)
            if (recipeUrl != null)
            {
                initializer.TriggerInitializationIfNotRunning(recipeUrl);
            }
            
            // wait for the initialization to complete
            // (returns immediately if already initialized)
            try
            {
                await initializer.WaitForFinishedInitialization(
                    // abort waiting if the HTTP request is cancelled
                    context.Request.CallCancelled
                );
            }
            catch (OperationCanceledException)
            {
                // either the request, or the initialization was cancelled
                
                // if it was the request, we don't do anything
                if (context.Request.CallCancelled.IsCancellationRequested)
                    return;
                
                // else if it was the initialization, terminate the request
                await RespondWith503InitializationCancelled(context);
                return;
            }
            catch (InitializationFailedException)
            {
                // the initialization was awaited, but it failed
                await RespondWith503InitializationFailed(context);
                return;
            }
            catch (InvalidOperationException)
            {
                // the initialization has not yet even started
                // (which does not make sense - we have just started it,
                // therefore it must have failed even before we began waiting)
                await RespondWith503InitializationFailed(context);
                return;
            }
            
            // now the worker is definitely initialized,
            // we can let the request continue
            await next(environment);
        }

        private async Task RespondWith409MissingRecipeUrl(IOwinContext ctx)
        {
            await ctx.SendError(
                statusCode: 409,
                errorNumber: 3000,
                "Worker is not initialized and no " +
                "initialization URL was provided with " +
                "the request.."
            );
        }

        private async Task RespondWith503InitializationFailed(IOwinContext ctx)
        {
            await ctx.SendError(
                statusCode: 503,
                errorNumber: 3001,
                "Worker initialization failed."
            );
        }

        private async Task RespondWith503InitializationCancelled(IOwinContext ctx)
        {
            await ctx.SendError(
                statusCode: 503,
                errorNumber: 3002,
                "Worker initialization was cancelled, " +
                "the worker is probably shutting down."
            );
        }
    }
}