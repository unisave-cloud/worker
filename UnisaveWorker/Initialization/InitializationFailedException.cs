using System;

namespace UnisaveWorker.Initialization
{
    /// <summary>
    /// Thrown by the Initializer when waiting for the initialization to finish,
    /// but the initialization fails with an exception.
    /// </summary>
    public class InitializationFailedException : Exception
    {
        public InitializationFailedException() : base(
            "Worker initialization has failed. See the worker console output."
        ) { }
    }
}