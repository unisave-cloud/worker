using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Watchdog.Execution;
using Watchdog.Http;
using Watchdog.Metrics;

namespace Watchdog
{
    /// <summary>
    /// Represents the watchdog server
    /// (which is composed of many components, http server being one of them)
    /// </summary>
    public class WatchdogServer : IDisposable
    {
        private readonly Config config;

        private readonly HealthStateManager healthStateManager;
        private readonly Initializer initializer;
        private readonly MetricsManager metricsManager;
        private readonly RequestQueue requestQueue;
        private readonly RequestConsumer requestConsumer;
        private readonly ExecutionKernel executionKernel;
        private readonly HttpClient httpClient;
        private readonly HttpServer httpServer;
        
        public WatchdogServer(Config config)
        {
            this.config = config;
            
            healthStateManager = new HealthStateManager();
            httpClient = new HttpClient();
            initializer = new Initializer(httpClient);
            metricsManager = new MetricsManager();
            requestQueue = new RequestQueue(healthStateManager, config.MaxQueueLength);
            executionKernel = new ExecutionKernel(
                healthStateManager,
                config.RequestTimeoutSeconds
            );
            requestConsumer = new RequestConsumer(
                requestQueue,
                initializer,
                healthStateManager,
                executionKernel
            );
            httpServer = new HttpServer(
                config.Port,
                new Router(healthStateManager, requestQueue, metricsManager)
            );
        }
        
        /// <summary>
        /// Start the server
        /// </summary>
        public void Start()
        {
            PrintStartupMessage();
            
            healthStateManager.Initialize();
            InitializeAsync().GetAwaiter().GetResult();
            executionKernel.Initialize();
            requestConsumer.Initialize();
            httpServer.Start();
            
            Log.Info("Unisave Watchdog running.");
        }
        
        private void PrintStartupMessage()
        {
            string version = typeof(WatchdogServer).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                .InformationalVersion;
            
            Console.WriteLine($"Starting Unisave Watchdog {version} ...");
            Console.WriteLine($"Listening on port {config.Port}");
            Console.WriteLine($"Execution timeout: {config.RequestTimeoutSeconds} seconds");
            Console.WriteLine("Process ID: " + Process.GetCurrentProcess().Id);
        }

        /// <summary>
        /// Downloads the game assemblies
        /// </summary>
        private async Task InitializeAsync()
        {
            // dummy init
            if (config.DummyInitialization)
            {
                initializer.DummyInitialization();
                return;
            }

            // regular init
            if (config.InitializationRecipeUrl == null)
                Log.Info("Skipping startup worker initialization.");
            else
                await initializer.InitializeWorker(config.InitializationRecipeUrl);
        }

        /// <summary>
        /// Stop the server
        /// </summary>
        public void Stop()
        {
            Log.Info("Stopping Unisave Watchdog...");
            
            httpServer?.Stop();
            requestConsumer?.Dispose();
            executionKernel?.Dispose();
            requestQueue?.Dispose();
            metricsManager?.Dispose();
            httpClient?.Dispose();
            healthStateManager?.Dispose();
            
            Log.Info("Bye.");
        }
        
        public void Dispose()
        {
            Stop();
        }
    }
}