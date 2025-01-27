using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnisaveWorker.Concurrency.Loop;
using UnisaveWorker.Initialization;

namespace UnisaveWorker.Concurrency
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
    
    /// <summary>
    /// Manages unisave request processing concurrency. Accepts configuration
    /// values and manages sub-middlewares that implement concurrency limits.
    /// </summary>
    public class ConcurrencyManagementMiddleware
    {
        // NOTE: changes to configuration are not handled in the nicest way
        // with regard to per-request alignment, but these settings should not
        // really change on a per-request basis, plus in the future, it will
        // all be set just once during startup and fixed for all requests.
        
        // ReSharper disable once InconsistentNaming
        private static readonly Version v0_11_0 = new("0.11.0.0");
        
        private readonly AppFunc next;
        private readonly BackendLoader backendLoader;
        
        private readonly RequestConcurrencyMiddleware requestConcurrencyMiddleware;
        private readonly LoopMiddleware loopMiddleware;
        
        /// <summary>
        /// Default concurrency settings to be used for the worker
        /// </summary>
        private readonly ConcurrencySettings defaultSettings;

        /// <summary>
        /// Concurrency settings currently used in this middleware instance
        /// </summary>
        private ConcurrencySettings currentSettings;
        
        // protects mutable state in this instance
        private readonly object syncLock = new object();

        public ConcurrencyManagementMiddleware(
            AppFunc next,
            ConcurrencySettings defaultSettings,
            LoopScheduler loopScheduler,
            BackendLoader backendLoader
        )
        {
            this.next = next;
            this.defaultSettings = defaultSettings;
            this.backendLoader = backendLoader;
            
            // request --> request limit --> thread limit --> next
            // (because the request limiter has an explicit queue)
            requestConcurrencyMiddleware = new RequestConcurrencyMiddleware(
                RequestConcurrencyMiddlewareCallback,
                defaultSettings.RequestConcurrency,
                defaultSettings.MaxQueueLength
            );
            loopMiddleware = new LoopMiddleware(
                next,
                loopScheduler
            );

            currentSettings = defaultSettings;
            ApplyCurrentSettings();
        }

        public async Task Invoke(IDictionary<string, object> environment)
        {
            // get settings to be used
            ConcurrencySettings desiredSettings = ExtractDesiredSettings(
                environment
            );
            
            // apply these settings to sub-middlewares
            lock (syncLock)
            {
                if (currentSettings != desiredSettings)
                {
                    currentSettings = desiredSettings;
                    ApplyCurrentSettings();
                }
            }
            
            // pass the request to the request concurrency middleware
            await requestConcurrencyMiddleware.Invoke(environment);
        }

        private async Task RequestConcurrencyMiddlewareCallback(
            IDictionary<string, object> environment
        )
        {
            // the request has passed through the request concurrency middleware
            
            // now pass it through the single-threaded loop (and then next),
            // or immediately through next:
            if (currentSettings.UseSingleThread)
                await loopMiddleware.Invoke(environment);
            else
                await next(environment);
        }

        /// <summary>
        /// Consults all the relevant places to obtain the concurrency settings
        /// that should be used for this request.
        /// </summary>
        private ConcurrencySettings ExtractDesiredSettings(
            IDictionary<string, object> environment
        )
        {
            // === start with the worker defaults ===
            
            int? requestConcurrency = defaultSettings.RequestConcurrency;
            bool useSingleThread = defaultSettings.UseSingleThread;
            int maxQueueLength = defaultSettings.MaxQueueLength;
            
            // === override based on the Unisave Framework version ===

            Version? frameworkVersion = backendLoader.UnisaveFrameworkVersion;

            if (frameworkVersion != null && frameworkVersion <= v0_11_0)
            {
                // Frameworks up to v0.11.0 were built to process one request
                // at a time, with synchronous Task awaiting, which means we
                // need multiple threads to avoid deadlocks.
                requestConcurrency = 1;
                useSingleThread = false;
            }
            
            // === override based on the unisave environment variables ===
            
            Dictionary<string, string> envDict
                = environment["worker.EnvDict"] as Dictionary<string, string>
                  ?? throw new Exception("Missing 'worker.EnvDict' value.");
            
            // override request concurrency
            if (envDict.TryGetValue(
                "WORKER_REQUEST_CONCURRENCY",
                out string? vRcc
            ))
                requestConcurrency = vRcc == "null" ? null : int.Parse(vRcc);
            
            // override single-threaded-ness
            if (envDict.TryGetValue(
                "WORKER_USE_SINGLE_THREAD",
                out string? vSt
            ))
                useSingleThread = bool.Parse(vSt);
            
            // override queue length
            if (envDict.TryGetValue(
                "WORKER_MAX_QUEUE_LENGTH",
                out string? vQl
            ))
                maxQueueLength = int.Parse(vQl);
            
            // == return the final values ===
            
            return new ConcurrencySettings(
                RequestConcurrency: requestConcurrency,
                UseSingleThread: useSingleThread,
                MaxQueueLength: maxQueueLength
            );
        }

        private void ApplyCurrentSettings()
        {
            requestConcurrencyMiddleware.SetMaxConcurrency(
                currentSettings.RequestConcurrency
            );
            requestConcurrencyMiddleware.SetMaxQueueLength(
                currentSettings.MaxQueueLength
            );
            
            Log.Info("Applying concurrency settings: " + currentSettings);
        }
    }
}