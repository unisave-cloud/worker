using System;
using Microsoft.Owin.Hosting;
using Watchdog;
using Watchdog.Metrics;

namespace UnisaveWorker
{
    public class WorkerApplication : IDisposable
    {
        private readonly Config config;

        private readonly HealthStateManager healthStateManager;
        private readonly MetricsManager metricsManager;

        private IDisposable httpServer;
        
        public WorkerApplication(Config config)
        {
            this.config = config;
            
            // TODO: construct all services
            healthStateManager = new HealthStateManager();
            metricsManager = new MetricsManager(config);
        }
        
        public void Start()
        {
            // PrintStartupMessage();
            
            // initialize services
            healthStateManager.Initialize();
            // InitializeAsync().GetAwaiter().GetResult();
            // executionKernel.Initialize();
            // requestConsumer.Initialize();
            
            // then start the HTTP server
            StartHttpServer();
            
            Log.Info("Unisave Watchdog running.");
        }

        private void StartHttpServer()
        {
            // pass services into the HTTP router
            var startup = new Startup(
                healthStateManager,
                metricsManager
            );
            
            string url = "http://*:" + config.Port;
            
            httpServer = WebApp.Start(url, startup.Configuration);
        }
        
        /// <summary>
        /// Stop the server
        /// </summary>
        public void Stop()
        {
            Log.Info("Stopping Unisave Worker...");
            
            // stop the HTTP server first
            httpServer?.Dispose();
            
            // then dispose services
            // requestConsumer?.Dispose();
            // executionKernel?.Dispose();
            // requestQueue?.Dispose();
            metricsManager?.Dispose();
            // httpClient?.Dispose();
            healthStateManager?.Dispose();
            
            Log.Info("Bye.");
        }
        
        public void Dispose()
        {
            Stop();
        }
    }
}