using System.Json;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;
using UnisaveWorker.Concurrency;
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

        public Startup(
            HealthStateManager healthStateManager,
            MetricsManager metricsManager,
            Initializer initializer
        )
        {
            this.metricsManager = metricsManager;
            this.initializer = initializer;
            this.healthStateManager = healthStateManager;
        }

        public void Configuration(IAppBuilder appBuilder)
        {
            // catches uncaught exceptions and logs them
            appBuilder.Use<ExceptionLoggingMiddleware>();
            
            // handle unisave requests
            appBuilder.MapWhen(
                ctx => ctx.Request.Method == "POST" &&
                       ctx.Request.Path.Value == "/",
                branch => branch
                    .Use<LegacyApiTranslationMiddleware>()
                    .Use<AccessLoggingMiddleware>()
                    // TODO: add middlewares for initialization and other stuff
                    // Wrap them into "ConcurrencyManagementMiddleware" that loads
                    // the concurrency from ENV and uses both internally as needed.
                    .Use<InitializationMiddleware>(initializer)
                    // TODO: legacy / new framework (backend) executor
                    .Run(ProcessRequest)
            );
            
            // handle other HTTP requests
            appBuilder.Route("GET", "/_/health", HealthCheck);
            appBuilder.Route("GET", "/metrics", Metrics);
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