using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Watchdog;

namespace UnisaveWorker.Initialization
{
    /// <summary>
    /// Primary service that encapsulates the worker initialization logic
    /// (the downloading of the game backend's DLLs and other assets)
    /// </summary>
    public abstract class Initializer : IDisposable
    {
        /// <summary>
        /// Path to the backend folder, into which the game backend DLLs
        /// and assets should be downloaded. When PerformInitialization
        /// method runs, this folder is already created and is guaranteed
        /// to be empty.
        /// </summary>
        public string BackendFolderPath
            => Path.Combine(Directory.GetCurrentDirectory(), "backend");
        
        /// <summary>
        /// Initialization state that the worker is currently in
        /// </summary>
        public InitializationState State { get; private set; }
        
        /// <summary>
        /// Lock used for synchronization
        /// </summary>
        private readonly object syncLock = new object();

        /// <summary>
        /// The task performing initialization, running on the .NET thread pool,
        /// set by the TriggerInitialization private method invocation
        /// </summary>
        private Task? initializationTask;
        
        /// <summary>
        /// Cancellation token source for the current initialization execution,
        /// null if there is no initialization running
        /// </summary>
        private CancellationTokenSource? initializationCts;
        
        /// <summary>
        /// Checks environment variables for the initialization recipe
        /// and then attempts to start eager initialization.
        /// Does nothing if those environment variables are not set up.
        /// </summary>
        public void AttemptEagerInitialization()
        {
            string? value = Environment.GetEnvironmentVariable(
                "INITIALIZATION_RECIPE_URL"
            );

            // do not eager initialize, if the variable is not set
            if (string.IsNullOrEmpty(value))
            {
                Log.Info("Skipping eager initialization: " +
                         "Environment variable is not set.");
                return;
            }
            
            // do not eager initialize, if the variable is not a URL
            if (!Uri.IsWellFormedUriString(value, UriKind.Absolute))
            {
                Log.Info("Skipping eager initialization: " +
                         "Environment variable is not a valid URI string.");
                return;
            }
            
            Log.Info("Triggering eager initialization...");
            TriggerInitializationIfNotRunning(value);
        }

        /// <summary>
        /// Starts initialization with the given recipe URL.
        /// If there already is an initialization running, or it has finished,
        /// it does nothing.
        /// </summary>
        /// <param name="recipeUrl">
        /// The URL from which to download the initialization recipe file
        /// </param>
        public void TriggerInitializationIfNotRunning(string recipeUrl)
        {
            lock (syncLock)
            {
                // any logic here occurs only if we are non initialized
                if (State != InitializationState.NonInitialized)
                    return;
                
                // update initializer state to reflect running initialization
                // and start the initialization task on the .NET thread pool
                State = InitializationState.BeingInitialized;
                initializationCts = new CancellationTokenSource();
                initializationTask = Task.Run(
                    () => TriggerInitialization(
                        recipeUrl,
                        initializationCts.Token
                    )
                );
            }
        }

        /// <summary>
        /// Calls the initialization method implementation
        /// (wraps it in exception handling and runs after-initialization
        /// state changes)
        /// </summary>
        private async Task TriggerInitialization(
            string recipeUrl,
            CancellationToken cancellationToken
        )
        {
            // log start
            var stopwatch = Stopwatch.StartNew();
            Log.Info("Starting initialization...");

            try
            {
                PrepareBackendFolder();
                
                // run the initialization itself
                await PerformInitialization(recipeUrl, cancellationToken);
            }
            catch (OperationCanceledException) // includes TaskCanceledException
            {
                // update initializer state back to uninitialized
                lock (syncLock)
                {
                    State = InitializationState.NonInitialized;
                    initializationTask = null;
                    initializationCts = null;
                }
                
                // log cancellation
                Log.Info("Initialization was cancelled.");
                
                // terminate initialization task with the cancellation exception
                throw;
            }
            catch (Exception e)
            {
                // update initializer state back to uninitialized
                lock (syncLock)
                {
                    State = InitializationState.NonInitialized;
                    initializationTask = null;
                    initializationCts = null;
                }
                
                // log the failure
                Log.Error($"Initialization failed: {e}");
                
                // terminate initialization task with proper exception
                throw new InitializationFailedException();
            }
            
            // log finish
            stopwatch.Stop();
            Log.Info(
                $"Worker initialized in {stopwatch.ElapsedMilliseconds}ms."
            );
                
            // update initializer state to successfully initialized
            lock (syncLock)
            {
                State = InitializationState.Initialized;
                initializationTask = null;
                initializationCts = null;
            }
        }

        /// <summary>
        /// Prepares an empty backend folder
        /// </summary>
        private void PrepareBackendFolder()
        {
            // remove the folder with contents if it exists
            if (Directory.Exists(BackendFolderPath))
                Directory.Delete(BackendFolderPath, recursive: true);
            
            // create the empty folder
            Directory.CreateDirectory(BackendFolderPath);
        }

        /// <summary>
        /// This method just asynchronously waits until the initialization
        /// finishes. If it already finished, it returns immediately.
        /// </summary>
        /// <param name="cancellationToken">
        /// Returns immediately when the token is triggered (stops waiting).
        /// No exception is thrown.
        /// </param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the initialization has not even started. You must trigger
        /// the initialization before waiting for its completion.
        /// </exception>
        /// <exception cref="InitializationFailedException">
        /// Thrown if the initialization has failed for some reason.
        /// </exception>
        public async Task WaitForFinishedInitialization(
            CancellationToken cancellationToken
        )
        {
            Task currentInitializationTcsTask;
            
            lock (syncLock) // 50ns overhead
            {
                switch (State)
                {
                    // if already initialized, return immediately
                    case InitializationState.Initialized:
                        return;
                    
                    // if being initialized, get the task to be awaited
                    case InitializationState.BeingInitialized:
                        currentInitializationTcsTask = initializationTask
                            ?? throw new Exception(
                                "Initializer state indicates that " +
                                "initialization task should exist, " +
                                "but it is null."
                            );
                        // and await it below the lock statement
                        break;
                    
                    // if not initialized, we cannot wait
                    case InitializationState.NonInitialized:
                        throw new InvalidOperationException(
                            "Cannot wait for initialization when there is no " +
                            "initialization currently running."
                        );
                    
                    // this should never happen
                    default:
                        throw new Exception(
                            "Unexpected initialization state value."
                        );
                }
            }

            // wait for the initialization to complete,
            // this may throw InitializationFailedException or
            // OperationCanceledException when the cancellation token is fired
            await currentInitializationTcsTask.ContinueWith(
                t => t.GetAwaiter().GetResult(),
                cancellationToken
            );
        }

        /// <summary>
        /// Override this method to implement the initialization process
        /// itself. Any uncaught exception from this method will be interpreted
        /// as a failed initialization. Running to completion is interpreted
        /// as a successful initialization.
        /// </summary>
        /// <param name="recipeUrl">
        /// The URL from which to download the initialization recipe
        /// </param>
        /// <param name="cancellationToken">
        /// Triggered when the worker shuts down. Give up on everything and
        /// leave it in whatever mess there is right now.
        /// </param>
        protected abstract Task PerformInitialization(
            string recipeUrl,
            CancellationToken cancellationToken
        );
        
        /// <summary>
        /// Stops the initialization process, if still running
        /// </summary>
        public virtual void Dispose()
        {
            lock (syncLock)
            {
                if (State == InitializationState.BeingInitialized)
                {
                    initializationCts?.Cancel();
                }
            }
        }
    }
}