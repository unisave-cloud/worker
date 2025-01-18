using System;

namespace UnisaveWorker.Initialization
{
    /// <summary>
    /// Thrown by the BackendLoader when accessing a value
    /// that is not available yet
    /// </summary>
    public class BackendNotLoadedException : Exception
    {
        public BackendNotLoadedException()
        {
        }

        public BackendNotLoadedException(string message) : base(message)
        {
        }

        public BackendNotLoadedException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}