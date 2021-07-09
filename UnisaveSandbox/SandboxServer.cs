using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using UnisaveSandbox.Http;

namespace UnisaveSandbox
{
    /// <summary>
    /// Represents the sandbox server
    /// (which is composed of many components, http server being one of them)
    /// </summary>
    public class SandboxServer : IDisposable
    {
        private readonly Config config;

        private readonly Initializer initializer;
        private readonly HttpClient httpClient;
        private readonly HttpServer httpServer;
        
        public SandboxServer(Config config)
        {
            this.config = config;
            
            httpClient = new HttpClient();

            initializer = new Initializer(httpClient);
            
            httpServer = new HttpServer(config.Port, new Router(initializer));
        }
        
        /// <summary>
        /// Start the server
        /// </summary>
        public void Start()
        {
            PrintStartupMessage();

            // TODO: should I accept HTTP requests before being initialized ???
            // -> what does the watchdog do if the fprocess crashes?
            InitializeAsync().GetAwaiter().GetResult();
            
            httpServer.Start();
            
            Log.Info("Unisave Sandbox running.");
        }
        
        private void PrintStartupMessage()
        {
            string version = typeof(SandboxServer).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                .InformationalVersion;
            
            Console.WriteLine($"Starting Unisave Sandbox {version} ...");
            Console.WriteLine($"Listening on port {config.Port}");
            Console.WriteLine("Process ID: " + Process.GetCurrentProcess().Id);
        }

        /// <summary>
        /// Downloads the game assemblies
        /// </summary>
        private async Task InitializeAsync()
        {
            if (config.InitializationRecipeUrl == null)
                Log.Info("Skipping startup sandbox initialization.");
            else
                await initializer.InitializeSandbox(config.InitializationRecipeUrl);
        }

        /// <summary>
        /// Stop the server
        /// </summary>
        public void Stop()
        {
            Log.Info("Stopping Unisave Sandbox...");
            
            httpClient?.Dispose();
            httpServer?.Stop();
            
            Log.Info("Bye.");
        }
        
        public void Dispose()
        {
            Stop();
        }
    }
}