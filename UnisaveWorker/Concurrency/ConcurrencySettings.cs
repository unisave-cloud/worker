namespace UnisaveWorker.Concurrency
{
    /// <summary>
    /// Aggregates all the settings that control worker's concurrency
    /// into one object that can be compared, defined and replaced as a unit.
    /// </summary>
    public record ConcurrencySettings(
        int? RequestConcurrency,
        bool UseSingleThread, 
        int MaxQueueLength
    )
    {
        /// <summary>
        /// How many requests can be processed at the same time.
        /// Null means unlimited.
        /// </summary>
        public int? RequestConcurrency { get; } = RequestConcurrency;
        
        /// <summary>
        /// Should requests be handled only by a single loop thread.
        /// </summary>
        public bool UseSingleThread { get; } = UseSingleThread;
        
        /// <summary>
        /// Maximum length of the waiting queue in front of the request
        /// concurrency limiter. When reached, requests will be rejected
        /// with HTTP 429.
        /// </summary>
        public int MaxQueueLength { get; } = MaxQueueLength;
    }
}