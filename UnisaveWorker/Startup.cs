using System.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;
using UnisaveWorker.Concurrency;
using UnisaveWorker.Execution;
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
        private readonly HealthStateManager healthStateManager;
        private readonly MetricsManager metricsManager;
        private readonly Initializer initializer;
        private readonly BackendLoader backendLoader;

        public Startup(
            HealthStateManager healthStateManager,
            MetricsManager metricsManager,
            Initializer initializer
        )
        {
            this.healthStateManager = healthStateManager;
            this.metricsManager = metricsManager;
            this.initializer = initializer;
            this.backendLoader = initializer.BackendLoader;
        }

        public void Configuration(IAppBuilder appBuilder)
        {
            // catches uncaught exceptions and logs them
            appBuilder.Use<ExceptionLoggingMiddleware>();

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
            
            // TODO: "ConcurrencyManagementMiddleware" that loads
            // the concurrency from ENV and uses both internally as needed.
            
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
                    .Run(ProcessRequest) // TODO ...
            );
        }

        private async Task ProcessRequest(IOwinContext context)
        {
            // TODO: actually process requests

            var body = new JsonObject() {
                ["status"] = "ok",
                ["returned"] = "DUMMY-RESPONSE",
                ["logs"] = new JsonArray()
            };
            
            await context.SendResponse(
                statusCode: 200,
                body: body.ToString(),
                contentType: "application/json"
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