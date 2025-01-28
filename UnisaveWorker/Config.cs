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
        /// unless overriden by the game backend or Unisave Framework version.
        /// Null means unlimited.
        /// </summary>
        public int? DefaultRequestConcurrency { get; private set; } = 10;

        /// <summary>
        /// Whether a single loop thread should be used by default to process
        /// requests, unless overriden by the game backend
        /// or Unisave Framework version.
        /// </summary>
        public bool DefaultUseSingleThread { get; private set; } = true;
        
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
        
        /// <summary>
        /// For how much time can the loop thread afford to work on a single
        /// task, before we consider the thread to be deadlocked.
        /// This option is not to "solve" deadlocks, just to cause the
        /// worker to recover and not get stuck. In properly written backend
        /// code, this should never trigger.
        /// </summary>
        public int LoopDeadlockTimeoutSeconds { get; private set; } = 30;
        
        /// <summary>
        /// How many bytes of memory usage are considered to be unhealthy
        /// and cause the worker to switch to the unhealthy state and be
        /// restarted. This prevents memory leaks from causing OOM crashes.
        /// </summary>
        public long UnhealthyMemoryUsageBytes { get; private set; }
            = 200 * 1024 * 1024; // 209715200
        // worker instances have 250 MB memory limit, so we set this at 200 MB
        
        
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
                DefaultUseSingleThread = GetEnvBool(
                    "WORKER_DEFAULT_USE_SINGLE_THREAD",
                    d.DefaultUseSingleThread
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
                ),
                LoopDeadlockTimeoutSeconds = GetEnvInteger(
                    "WORKER_LOOP_DEADLOCK_TIMEOUT_SECONDS",
                    d.LoopDeadlockTimeoutSeconds
                ),
                UnhealthyMemoryUsageBytes = GetEnvLong(
                    "WORKER_UNHEALTHY_MEMORY_USAGE_BYTES",
                    d.UnhealthyMemoryUsageBytes
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
        
        private static long GetEnvLong(string key, long defaultValue = 0L)
        {
            string? s = GetEnvString(key);

            if (string.IsNullOrEmpty(s))
                return defaultValue;
            
            if (long.TryParse(s, out long l))
                return l;

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