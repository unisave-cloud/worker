using System;
using System.Net.Http;
using Microsoft.Owin.Hosting;
using UnisaveWorker.Initialization;
using Watchdog;
using Watchdog.Metrics;

namespace UnisaveWorker
{
    public class WorkerApplication : IDisposable
    {
        private readonly Config config;

        private readonly HealthStateManager healthStateManager;
        private readonly MetricsManager metricsManager;
        private readonly HttpClient httpClient;
        private readonly UnisaveWorker.Initialization.Initializer initializer;

        private IDisposable? httpServer;
        
        public WorkerApplication(Config config)
        {
            this.config = config;
            
            // TODO: construct all services
            healthStateManager = new HealthStateManager();
            metricsManager = new MetricsManager(config);
            httpClient = new HttpClient();
            initializer = new RecipeV1Initializer(httpClient);
        }
        
        public void Start()
        {
            // PrintStartupMessage();
            
            // initialize services
            healthStateManager.Initialize();
            initializer.AttemptEagerInitialization();
            // executionKernel.Initialize();
            // requestConsumer.Initialize();
            
            // then start the HTTP server
            StartHttpServer();
            
            Log.Info("Unisave Worker running.");
        }

        private void StartHttpServer()
        {
            // pass services into the HTTP router
            var startup = new Startup(
                healthStateManager,
                metricsManager,
                initializer
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
            initializer.Dispose();
            metricsManager.Dispose();
            httpClient.Dispose();
            healthStateManager.Dispose();
            
            Log.Info("Bye.");
        }
        
        public void Dispose()
        {
            Stop();
        }
    }
}