using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnisaveWorker.Concurrency
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
    
    /// <summary>
    /// Wraps the two concurrency middlewares and updates their configuration
    /// based on the currently active per-request unisave environment variables
    /// (in the future, these will be just loaded from configuration
    /// and kept constant)
    /// </summary>
    public class ConcurrencyManagementMiddleware
    {
        private readonly RequestConcurrencyMiddleware requestMiddleware;
        private readonly ThreadConcurrencyMiddleware threadMiddleware;
        
        private readonly AppFunc wrappedNext;
        
        /// <summary>
        /// Captures the concurrency settings state
        /// </summary>
        public record State(int? RequestCc, int? ThreadCc, int MaxQueueLength)
        {
            public int? RequestCc { get; } = RequestCc;
            public int? ThreadCc { get; } = ThreadCc;
            public int MaxQueueLength { get; } = MaxQueueLength;
        }

        private readonly State defaultState;

        private State currentState;
        private readonly object syncLock = new object();

        public ConcurrencyManagementMiddleware(
            AppFunc next,
            State defaultState
        )
        {
            // request --> request limit --> thread limit --> next
            // (because the request limiter has an explicit queue)
            threadMiddleware = new ThreadConcurrencyMiddleware(
                next,
                defaultState.ThreadCc
            );
            requestMiddleware = new RequestConcurrencyMiddleware(
                threadMiddleware.Invoke,
                defaultState.RequestCc,
                defaultState.MaxQueueLength
            );
            wrappedNext = requestMiddleware.Invoke;

            this.defaultState = defaultState;
            
            currentState = defaultState;
            ApplyCurrentState();
        }

        public async Task Invoke(IDictionary<string, object> environment)
        {
            State desiredState = ExtractDesiredState(environment);
            
            lock (syncLock)
            {
                if (currentState != desiredState)
                {
                    currentState = desiredState;
                    ApplyCurrentState();
                }
            }
            
            // pass the request through concurrency limiters
            // and further down the processing pipeline
            await wrappedNext(environment);
        }

        private State ExtractDesiredState(
            IDictionary<string, object> environment
        )
        {
            Dictionary<string, string> envDict
                = environment["worker.EnvDict"] as Dictionary<string, string>
                ?? throw new Exception("Missing 'worker.EnvDict' value.");

            int? requestCc = GetIntEnvVar(envDict, "WORKER_REQUEST_CONCURRENCY");
            int? threadCc = GetIntEnvVar(envDict, "WORKER_THREAD_CONCURRENCY");
            int? maxQueueLength = GetIntEnvVar(envDict, "WORKER_MAX_QUEUE_LENGTH");

            return new State(
                RequestCc: requestCc ?? defaultState.RequestCc,
                ThreadCc: threadCc ?? defaultState.ThreadCc,
                MaxQueueLength: maxQueueLength ?? defaultState.MaxQueueLength
            );
        }

        private int? GetIntEnvVar(Dictionary<string, string> envDict, string key)
        {
            if (!envDict.TryGetValue(key, out string? stringValue))
                return null;
            
            if (int.TryParse(stringValue, out int intValue))
                return intValue;

            return null;
        }

        private void ApplyCurrentState()
        {
            requestMiddleware.SetMaxConcurrency(currentState.RequestCc);
            requestMiddleware.SetMaxQueueLength(currentState.MaxQueueLength);
            
            threadMiddleware.SetMaxConcurrency(currentState.ThreadCc);
            
            Log.Info("Using concurrency options: " + currentState);
        }
    }
}