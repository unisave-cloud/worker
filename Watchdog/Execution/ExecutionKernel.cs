using System;

namespace Watchdog.Execution
{
    public class ExecutionKernel : IDisposable
    {
        private readonly HealthStateManager healthStateManager;
        
        private readonly TimeoutWrapper timeoutWrapper;
        private readonly int timeoutSeconds;

        public ExecutionKernel(
            HealthStateManager healthStateManager,
            int timeoutSeconds
        )
        {
            this.healthStateManager = healthStateManager;
            this.timeoutSeconds = timeoutSeconds;

            timeoutWrapper = new TimeoutWrapper(timeoutSeconds);
        }

        public void Initialize()
        {
            timeoutWrapper.Initialize();
        }

        public void Dispose()
        {
            timeoutWrapper?.Dispose();
        }

        /// <summary>
        /// Gets an execution request and returns an execution response.
        ///
        /// If this method throws an exception, it means something went wrong
        /// with the worker code. User code shouldn't be able to cause this
        /// method to throw.
        /// </summary>
        public ExecutionResponse Handle(ExecutionRequest executionRequest)
        {
            if (executionRequest == null)
                throw new ArgumentNullException(nameof(executionRequest));
            
            var response = new ExecutionResponse();
            
            // TODO: meter network and memory usage

            string executionResultFromInnerThread = null;
            bool timedOut = timeoutWrapper.Run(() => {
                
                var executor = new Executor();
                executionResultFromInnerThread = executor.ExecuteBackend(
                    executionRequest.ExecutionParameters
                );
                
            });

            if (timedOut)
            {
                Log.Error("Execution request timed out! Killing the whole worker...");
                
                response.ExecutionResult = FormatTimeoutResponse();
                
                // there's a wild thread running somewhere
                healthStateManager.SetUnhealthy();
            }
            else
            {
                response.ExecutionResult = executionResultFromInnerThread;
            }

            return response;
        }

        private string FormatTimeoutResponse()
        {
            string message = $"The backend execution ran for too long " +
                             $"and exceeded the execution timeout " +
                             $"of {timeoutSeconds} seconds.";
            
            return @"{
                'result': 'exception',
                'exception': {
                    'ClassName': 'System.TimeoutException',
                    'Message': '###',
                    'StackTraceString': '   at UnisaveWorker'
                },
                'special': {}
            }".Replace('\'', '\"').Replace("###", message);
        }
    }
}