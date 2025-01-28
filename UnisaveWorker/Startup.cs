using System.Threading;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;
using UnisaveWorker.Concurrency;
using UnisaveWorker.Concurrency.Loop;
using UnisaveWorker.Execution;
using UnisaveWorker.Ingress;
using UnisaveWorker.Initialization;
using UnisaveWorker.Metrics;

namespace UnisaveWorker
{
    /// <summary>
    /// Worker HTTP endpoints and their processing pipeline is defined here
    /// </summary>
    public class Startup
    {
        private readonly Config config;
        
        private readonly GracefulShutdownManager shutdownManager;
        private readonly MetricsManager metricsManager;
        private readonly Initializer initializer;
        private readonly BackendLoader backendLoader;
        private readonly LoopScheduler loopScheduler;

        public Startup(
            Config config,
            GracefulShutdownManager shutdownManager,
            MetricsManager metricsManager,
            Initializer initializer,
            LoopScheduler loopScheduler
        )
        {
            this.config = config;
            this.metricsManager = metricsManager;
            this.initializer = initializer;
            this.loopScheduler = loopScheduler;
            this.shutdownManager = shutdownManager;
            this.backendLoader = initializer.BackendLoader;
        }

        public void Configuration(IAppBuilder appBuilder)
        {
            // sets the "Server" response header
            appBuilder.Use<ServerResponseHeaderMiddleware>();
            
            // catches uncaught exceptions and logs them
            appBuilder.Use<ExceptionLoggingMiddleware>();
            
            // implements a graceful shutdown period
            appBuilder.Use<GracefulShutdownMiddleware>(shutdownManager);

            // handle unisave requests
            appBuilder.MapWhen(
                ctx => ctx.Request.Method == "POST" &&
                       ctx.Request.Path.Value == "/",
                DefineUnisaveRequestProcessingBranch
            );
            
            // handle other HTTP requests
            appBuilder.Route("GET", "/_/health", HealthCheck);
            appBuilder.Route("GET", "/metrics", Metrics);
        }

        private void DefineUnisaveRequestProcessingBranch(IAppBuilder branch)
        {
            branch.Use<LegacyApiTranslationMiddleware>();
            
            branch.Use<AccessLoggingMiddleware>(metricsManager);
            
            branch.Use<InitializationMiddleware>(initializer);

            branch.Use<ConcurrencyManagementMiddleware>(
                new ConcurrencySettings(
                    RequestConcurrency: config.DefaultRequestConcurrency,
                    UseSingleThread: config.DefaultUseSingleThread,
                    MaxQueueLength: config.DefaultMaxQueueLength
                ),
                loopScheduler,
                backendLoader
            );

            branch.Use<ExecutionTimingMiddleware>();
            
            // Unisave request execution via the OWIN entrypoint
            branch.MapWhen(
                _ => backendLoader.HasOwinStartupConfigurationMethod,
                b => b.Use<OwinStartupExecutionMiddleware>(
                    backendLoader,
                    (CancellationToken) branch.Properties["host.OnAppDisposing"]
                )
            );
            
            // Unisave request execution via the legacy framework entrypoint
            branch.MapWhen(
                _ => backendLoader.HasLegacyStartMethod,
                b => b.Use<LegacyEntrypointExecutionMiddleware>(backendLoader)
            );
        }

        private async Task HealthCheck(IOwinContext context)
        {
            // The goal of a health check is to verify that the worker is
            // able to respond to HTTP requests (i.e. to detect memory or
            // CPU related issues). Therefore, we just reply that we are healthy.
            
            // NOTE: During shutdown, the graceful shutdown middleware will
            // respond with 503, which signals unhealthy worker.
            // However, that's when we are shutting down any ways, so it's true.
            
            await context.SendResponse(200, "OK\n");
        }
        
        private async Task Metrics(IOwinContext context)
        {
            string metrics = metricsManager.ToPrometheusTextFormat();

            await context.SendResponse(200, metrics);
        }
    }
}