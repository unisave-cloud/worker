using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;
using UnisaveWorker.Concurrency;
using Watchdog;
using Watchdog.Metrics;

namespace UnisaveWorker
{
    /// <summary>
    /// Worker HTTP endpoints and their processing pipeline is defined here
    /// </summary>
    public class Startup
    {
        private readonly HealthStateManager healthStateManager;
        private readonly MetricsManager metricsManager;

        public Startup(
            HealthStateManager healthStateManager,
            MetricsManager metricsManager
        )
        {
            this.metricsManager = metricsManager;
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
                    // TODO: add middlewares for initialization and other stuff
                    // Wrap them into "ConcurrencyManagementMiddleware" that loads
                    // the concurrency from ENV and uses both internally as needed.
                    .Run(ProcessRequest)
            );
            
            // handle other HTTP requests
            appBuilder.Route("GET", "/_/health", HealthCheck);
            appBuilder.Route("GET", "/metrics", Metrics);
        }

        private async Task ProcessRequest(IOwinContext context)
        {
            // TODO: process requests
            await context.SendResponse(200, "TODO: process requests\n");
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