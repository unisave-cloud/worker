using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Owin.Builder;
using Owin;
using UnisaveWorker.Initialization;

namespace UnisaveWorker.Execution
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
    
    /// <summary>
    /// Executes game backend via the OWIN startup class
    /// </summary>
    public class OwinStartupExecutionMiddleware
    {
        private readonly BackendLoader backendLoader;
        
        private record BackendApplication(
            string EnvString,
            AppFunc AppFunc,
            CancellationTokenSource StoppingToken
        )
        {
            public string EnvString { get; } = EnvString;
            public AppFunc AppFunc { get; } = AppFunc;
            public CancellationTokenSource StoppingToken { get; } = StoppingToken;
        }
        
        /// <summary>
        /// Currently used backend application.
        /// If null, application must be created before first use.
        /// </summary>
        private BackendApplication? currentBackendApplication;
        
        /// <summary>
        /// Synchronizes access to the backend application
        /// </summary>
        private readonly object syncLock = new object();
        
        public OwinStartupExecutionMiddleware(
            AppFunc next, // not used
            BackendLoader backendLoader,
            CancellationToken workerStoppingToken
        )
        {
            this.backendLoader = backendLoader;

            workerStoppingToken.Register(OnWorkerStopping);
        }

        public async Task Invoke(IDictionary<string, object> environment)
        {
            IDictionary<string, string> envDict
                = environment["worker.EnvDict"] as IDictionary<string, string>
                ?? throw new Exception("Missing 'worker.EnvDict' value.");
            string envString = environment["worker.EnvString"] as string
                ?? throw new Exception("Missing 'worker.EnvString' value.");
            
            AppFunc backendAppFunc = ResolveBackendAppFunc(
                envDict,
                envString
            );
            
            await backendAppFunc(environment);
        }

        public void OnWorkerStopping()
        {
            // dispose the current backend app (if one exists)
            lock (syncLock)
            {
                if (currentBackendApplication != null)
                {
                    currentBackendApplication.StoppingToken.Cancel();
                    currentBackendApplication = null;
                }
            }
        }

        private AppFunc ResolveBackendAppFunc(
            IDictionary<string, string> envDict,
            string envString
        )
        {
            lock (syncLock)
            {
                // create if missing
                if (currentBackendApplication == null)
                {
                    Log.Info(
                        "Creating backend application for execution via OWIN..."
                    );
                    
                    currentBackendApplication = ConstructBackendApplication(
                        envDict, envString
                    );
                }
                
                // change if env vars changed
                if (currentBackendApplication.EnvString != envString)
                {
                    Log.Info(
                        "Environment variables changed, reloading..."
                    );
                    
                    currentBackendApplication.StoppingToken.Cancel();
                    currentBackendApplication = ConstructBackendApplication(
                        envDict, envString
                    );
                }

                return currentBackendApplication.AppFunc;
            }
        }

        private BackendApplication ConstructBackendApplication(
            IDictionary<string, string> envDict,
            string envString
        )
        {
            var stoppingToken = new CancellationTokenSource();
            
            var appBuilder = new AppBuilder {
                Properties = {
                    // OWIN properties
                    ["owin.Version"] = "1.0.0",
                    ["host.OnAppDisposing"] = stoppingToken.Token,
                    
                    // unisave properties
                    ["unisave.GameAssemblies"] = backendLoader.GameAssemblies
                        .ToArray(),
                    ["unisave.EnvironmentVariables"] = envDict
                }
            };
            
            object? startupClassInstance = backendLoader
                .OwinStartupConfigurationMethod
                .DeclaringType
                ?.GetConstructor(Type.EmptyTypes)
                ?.Invoke(Array.Empty<object>());

            var startup = (Action<IAppBuilder>) Delegate.CreateDelegate(
                typeof(Action<IAppBuilder>),
                startupClassInstance,
                backendLoader.OwinStartupConfigurationMethod
            ) ?? throw new Exception("Could not create the startup action.");
            
            startup.Invoke(appBuilder);

            AppFunc appFunc = appBuilder.Build<AppFunc>();

            return new BackendApplication(
                EnvString: envString,
                AppFunc: appFunc,
                StoppingToken: stoppingToken
            );
        }
    }
}