using System;

namespace Watchdog
{
    public class Config
    {
        /// <summary>
        /// What port to listen on with the HTTP server
        /// </summary>
        public int Port { get; set; } = 8080;

        /// <summary>
        /// Where to download the initialization recipe from
        /// May be null, then the URL need to be sent with each request
        /// or the dummy initialization is taking place
        /// </summary>
        public string InitializationRecipeUrl { get; set; }
        
        /// <summary>
        /// Initialize the worker to dummy framework and game for debugging
        /// outside the rest of the unisave system
        /// </summary>
        public bool DummyInitialization { get; set; }

        /// <summary>
        /// Maximum length of the synchronizing request queue
        /// </summary>
        public int MaxQueueLength { get; set; } = 20;

        /// <summary>
        /// Timeout for request execution in seconds
        /// </summary>
        public int RequestTimeoutSeconds { get; set; } = 5;
        
        /// <summary>
        /// Environment ID of the worker pool,
        /// may be empty and is empty for eager pools
        /// </summary>
        public string WorkerEnvironmentId { get; set; }
        
        /// <summary>
        /// Backend ID of the worker pool,
        /// may be empty and is empty for eager pools
        /// </summary>
        public string WorkerBackendId { get; set; }
        
        /// <summary>
        /// The HTTP server should print a lot of additional information
        /// </summary>
        public bool VerboseHttpServer { get; set; }

        /// <summary>
        /// Default request concurrency level to use,
        /// unless overriden by the game backend.
        /// Null means unlimited.
        /// </summary>
        public int? DefaultRequestConcurrency { get; set; } = null;

        /// <summary>
        /// Default thread concurrency level to use,
        /// unless overriden by the game backend.
        /// Null means unlimited.
        /// </summary>
        public int? DefaultThreadConcurrency { get; set; } = null;
        
        /// <summary>
        /// Default value for the maximum request queue length,
        /// unless overriden by the game backend.
        /// </summary>
        public int DefaultMaxQueueLength { get; set; } = 20;

        /// <summary>
        /// Friendly name of the OwinStartupAttribute used to find the Startup
        /// class inside the game's assemblies
        /// </summary>
        public string OwinStartupAttribute { get; set; } = "UnisaveFramework";
        
        
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
                Port = GetEnvInteger(
                    "WATCHDOG_SERVER_PORT", d.Port
                ),
                InitializationRecipeUrl = GetEnvString(
                    "INITIALIZATION_RECIPE_URL"
                ),
                DummyInitialization = GetEnvBool(
                    "WATCHDOG_DUMMY_INITIALIZATION", false
                ),
                MaxQueueLength = GetEnvInteger(
                    "MAX_QUEUE_LENGTH", d.MaxQueueLength
                ),
                RequestTimeoutSeconds = GetEnvInteger(
                    "REQUEST_TIMEOUT_SECONDS", d.RequestTimeoutSeconds
                ),
                WorkerEnvironmentId = GetEnvString(
                    "WORKER_ENVIRONMENT_ID"
                ),
                WorkerBackendId = GetEnvString(
                    "WORKER_BACKEND_ID"
                ),
                VerboseHttpServer = GetEnvBool(
                    "VERBOSE_HTTP_SERVER", false
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
                )
            };
        }

        private static int? GetEnvNullableInteger(string key, int? defaultValue = null)
        {
            string s = GetEnvString(key);

            if (string.IsNullOrEmpty(s))
                return defaultValue;
            
            if (int.TryParse(s, out int i))
                return i;

            return defaultValue;
        }

        private static int GetEnvInteger(string key, int defaultValue = 0)
        {
            string s = GetEnvString(key);

            if (string.IsNullOrEmpty(s))
                return defaultValue;
            
            if (int.TryParse(s, out int i))
                return i;

            return defaultValue;
        }

        private static bool GetEnvBool(string key, bool defaultValue = false)
        {
            string s = GetEnvString(key);
            
            if (string.IsNullOrEmpty(s))
                return defaultValue;
            
            if (bool.TryParse(s, out bool b))
                return b;
            
            return defaultValue;
        }

        private static string GetEnvString(string key, string defaultValue = null)
        {
            string v = Environment.GetEnvironmentVariable(key);
            
            if (string.IsNullOrEmpty(v))
                v = defaultValue;
            
            return v;
        }
    }
}