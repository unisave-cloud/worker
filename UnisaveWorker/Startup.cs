using System.Threading;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;
using UnisaveWorker.Concurrency;
using UnisaveWorker.Execution;
using UnisaveWorker.Ingress;
using UnisaveWorker.Initialization;
using Watchdog;
using Watchdog.Metrics;
using Initializer = UnisaveWorker.Initialization.Initializer;

namespace UnisaveWorker
{
    /// <summary>
    /// Worker HTTP endpoints and their processing pipeline is defined here
    /// </summary>
    public class Startup
    {
        private readonly Config config;
        
        private readonly GracefulShutdownManager shutdownManager;
        private readonly HealthStateManager healthStateManager;
        private readonly MetricsManager metricsManager;
        private readonly Initializer initializer;
        private readonly BackendLoader backendLoader;

        public Startup(
            Config config,
            GracefulShutdownManager shutdownManager,
            HealthStateManager healthStateManager,
            MetricsManager metricsManager,
            Initializer initializer
        )
        {
            this.config = config;
            this.healthStateManager = healthStateManager;
            this.metricsManager = metricsManager;
            this.initializer = initializer;
            this.shutdownManager = shutdownManager;
            this.backendLoader = initializer.BackendLoader;
        }

        public void Configuration(IAppBuilder appBuilder)
        {
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
            
            branch.Use<AccessLoggingMiddleware>();

            branch.Use<ConcurrencyManagementMiddleware>(
                new ConcurrencyManagementMiddleware.State(
                    RequestCc: config.DefaultRequestConcurrency,
                    ThreadCc: config.DefaultThreadConcurrency,
                    MaxQueueLength: config.DefaultMaxQueueLength
                )
            );
            
            branch.Use<InitializationMiddleware>(initializer);
            
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
                b => b
                    .Use<RequestConcurrencyMiddleware>(
                        /* concurrency: */ 1,
                        /* max queue length: */ 20
                    )
                    .Use<LegacyEntrypointExecutionMiddleware>(backendLoader)
            );
        }

        private async Task HealthCheck(IOwinContext context)
        {
            if (healthStateManager.IsHealthy())
            {
                await context.SendResponse(200, "OK\n");
            }
            else
            {
                await context.SendResponse(503, "Service Unavailable\n");
            }
        }
        
        private async Task Metrics(IOwinContext context)
        {
            string metrics = metricsManager.ToPrometheusTextFormat();

            await context.SendResponse(200, metrics);
        }
    }
}