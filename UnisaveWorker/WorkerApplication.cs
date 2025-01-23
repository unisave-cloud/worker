using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using Microsoft.Owin.Hosting;
using UnisaveWorker.Concurrency;
using UnisaveWorker.Concurrency.Loop;
using UnisaveWorker.Ingress;
using UnisaveWorker.Initialization;
using UnisaveWorker.Metrics;

namespace UnisaveWorker
{
    /// <summary>
    /// Represents the whole Unisave Worker Application,
    /// this class handles service construction, lifetime, and their composition
    /// </summary>
    public class WorkerApplication : IDisposable
    {
        private readonly Config config;

        private readonly GracefulShutdownManager shutdownManager;
        private readonly MetricsManager metricsManager;
        private readonly HttpClient httpClient;
        private readonly Initializer initializer;
        private readonly LoopScheduler loopScheduler;

        private IDisposable? httpServer;
        
        public WorkerApplication(Config config)
        {
            this.config = config;
            
            shutdownManager = new GracefulShutdownManager();
            metricsManager = new MetricsManager(
                workerEnvironmentId: config.WorkerEnvironmentId,
                workerBackendId: config.WorkerBackendId
            );
            httpClient = new HttpClient();
            initializer = new RecipeV1Initializer(
                httpClient,
                config.OwinStartupAttribute
            );
            loopScheduler = new LoopScheduler();
        }
        
        public void Start()
        {
            PrintStartupMessage();
            
            // initialize services
            initializer.AttemptEagerInitialization(
                config.InitializationRecipeUrl
            );
            
            // then start the HTTP server
            StartHttpServer();
            
            Log.Info("Unisave Worker running.");
        }
        
        private void PrintStartupMessage()
        {
            string version = typeof(WorkerApplication).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                .InformationalVersion;
            
            Log.Info($"Starting Unisave Worker {version} ...");
            Log.Info($"Listening at URL {config.HttpUrl}");
            Log.Info("Process ID: " + Process.GetCurrentProcess().Id);
        }

        private void StartHttpServer()
        {
            // pass services into the HTTP router
            var startup = new Startup(
                config,
                shutdownManager,
                metricsManager,
                initializer,
                loopScheduler
            );
            
            httpServer = WebApp.Start(
                url: config.HttpUrl, // e.g. "http://*:8080"
                startup: startup.Configuration
            );
        }
        
        /// <summary>
        /// Waits for pending requests to finish, meanwhile rejects new requests
        /// </summary>
        public void Stop()
        {
            Log.Info("Stopping Unisave Worker...");
            
            // wait for pending requests to finish
            // with a maximum timeout
            shutdownManager.PerformGracefulShutdown(
                TimeSpan.FromSeconds(config.GracefulShutdownSeconds)
            );
            
            // stop the HTTP server
            httpServer?.Dispose();
            Log.Info("HTTP server stopped.");
        }
        
        public void Dispose()
        {
            loopScheduler.Dispose();
            initializer.Dispose();
            metricsManager.Dispose();
            httpClient.Dispose();
            
            Log.Info("Bye.");
        }
    }
}