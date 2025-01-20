using System;
// ReSharper disable RedundantDefaultMemberInitializer

namespace UnisaveWorker
{
    public class Config
    {
        /// <summary>
        /// What IP and Port to listen on with the HTTP server,
        /// e.g. `http://*:8080`
        /// </summary>
        public string HttpUrl { get; private set; } = "http://*:8080";

        /// <summary>
        /// Where to download the initialization recipe from
        /// May be null, then the URL need to be sent with each request
        /// or the dummy initialization is taking place
        /// </summary>
        public string? InitializationRecipeUrl { get; private set; } = null;
        
        /// <summary>
        /// Environment ID of the worker pool,
        /// may be empty and is empty for eager pools.
        /// Used for prometheus metrics.
        /// </summary>
        public string? WorkerEnvironmentId { get; private set; } = null;

        /// <summary>
        /// Backend ID of the worker pool,
        /// may be empty and is empty for eager pools.
        /// Used for prometheus metrics.
        /// </summary>
        public string? WorkerBackendId { get; private set; } = null;

        /// <summary>
        /// Default request concurrency level to use,
        /// unless overriden by the game backend.
        /// Null means unlimited.
        /// </summary>
        public int? DefaultRequestConcurrency { get; private set; } = 500;

        /// <summary>
        /// Default thread concurrency level to use,
        /// unless overriden by the game backend.
        /// Null means unlimited.
        /// </summary>
        public int? DefaultThreadConcurrency { get; private set; } = null;
        
        /// <summary>
        /// Default value for the maximum request queue length,
        /// unless overriden by the game backend.
        /// </summary>
        public int DefaultMaxQueueLength { get; private set; } = 20;

        /// <summary>
        /// Friendly name of the OwinStartupAttribute used to find the Startup
        /// class inside the game's assemblies
        /// </summary>
        public string OwinStartupAttribute { get; private set; }
            = "UnisaveFramework";
        
        /// <summary>
        /// How many seconds at most should the worker wait for pending requests
        /// when it's being terminated.
        /// </summary>
        public int GracefulShutdownSeconds { get; private set; } = 10;
        
        
        /////////////
        // Methods //
        /////////////

        /// <summary>
        /// Loads worker configuration from environment variables
        /// </summary>
        /// <returns></returns>
        public static Config LoadFromEnv()
        {
            var d = new Config();
            
            return new Config {
                HttpUrl = GetEnvString(
                    "WORKER_HTTP_URL", d.HttpUrl
                )!,
                InitializationRecipeUrl = GetEnvString(
                    "INITIALIZATION_RECIPE_URL"
                ),
                WorkerEnvironmentId = GetEnvString(
                    "WORKER_ENVIRONMENT_ID"
                ),
                WorkerBackendId = GetEnvString(
                    "WORKER_BACKEND_ID"
                ),
                DefaultRequestConcurrency = GetEnvNullableInteger(
                    "WORKER_DEFAULT_REQUEST_CONCURRENCY",
                    d.DefaultRequestConcurrency
                ),
                DefaultThreadConcurrency = GetEnvNullableInteger(
                    "WORKER_DEFAULT_THREAD_CONCURRENCY",
                    d.DefaultThreadConcurrency
                ),
                DefaultMaxQueueLength = GetEnvInteger(
                    "WORKER_DEFAULT_MAX_QUEUE_LENGTH",
                    d.DefaultMaxQueueLength
                ),
                OwinStartupAttribute = GetEnvString(
                    "WORKER_OWIN_STARTUP_ATTRIBUTE",
                    d.OwinStartupAttribute
                )!,
                GracefulShutdownSeconds = GetEnvInteger(
                    "WORKER_GRACEFUL_SHUTDOWN_SECONDS",
                    d.GracefulShutdownSeconds
                )
            };
        }

        private static int? GetEnvNullableInteger(string key, int? defaultValue = null)
        {
            string? s = GetEnvString(key);

            if (string.IsNullOrEmpty(s))
                return defaultValue;

            if (s!.ToLowerInvariant() == "null")
                return null;
            
            if (int.TryParse(s, out int i))
                return i;

            return defaultValue;
        }

        private static int GetEnvInteger(string key, int defaultValue = 0)
        {
            string? s = GetEnvString(key);

            if (string.IsNullOrEmpty(s))
                return defaultValue;
            
            if (int.TryParse(s, out int i))
                return i;

            return defaultValue;
        }

        private static bool GetEnvBool(string key, bool defaultValue = false)
        {
            string? s = GetEnvString(key);
            
            if (string.IsNullOrEmpty(s))
                return defaultValue;
            
            if (bool.TryParse(s, out bool b))
                return b;
            
            return defaultValue;
        }

        private static string? GetEnvString(string key, string? defaultValue = null)
        {
            string? v = Environment.GetEnvironmentVariable(key);
            
            if (string.IsNullOrEmpty(v))
                v = defaultValue;
            
            return v;
        }
    }
}