using System;
using System.Threading;
using System.Threading.Tasks;

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
        public string BackendFolderPath => throw new NotImplementedException();
        
        /// <summary>
        /// Initialization state that the worker is currently in
        /// </summary>
        public InitializationState State => throw new NotImplementedException();
        
        /// <summary>
        /// Checks environment variables for the initialization recipe
        /// and then attempts to start eager initialization.
        /// Does nothing if those environment variables are not set up.
        /// </summary>
        public void AttemptEagerInitialization()
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
        public Task WaitForFinishedInitialization(
            CancellationToken cancellationToken
        )
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }
    }
}