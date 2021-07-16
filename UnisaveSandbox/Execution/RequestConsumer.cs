using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnisaveSandbox.Http;

namespace UnisaveSandbox.Execution
{
    public class RequestConsumer : IDisposable
    {
        private readonly Initializer initializer;
        private readonly RequestQueue requestQueue;
        private readonly HealthStateManager healthStateManager;
        private readonly ExecutionKernel executionKernel;

        private CancellationTokenSource loopCancellation;
        private Thread loopThread;

        public RequestConsumer(
            RequestQueue requestQueue,
            Initializer initializer,
            HealthStateManager healthStateManager,
            ExecutionKernel executionKernel
        )
        {
            this.requestQueue = requestQueue;
            this.initializer = initializer;
            this.healthStateManager = healthStateManager;
            this.executionKernel = executionKernel;
        }

        public void Initialize()
        {
            loopCancellation = new CancellationTokenSource();
            
            loopThread = new Thread(ConsumingLoop);
            loopThread.Start();
        }
        
        private void ConsumingLoop()
        {
            while (true)
            {
                HttpListenerContext context = null;
                
                try
                {
                    context = requestQueue.DequeueRequest(loopCancellation.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                if (context == null)
                    break;
                
                try
                {
                    HandleRequest(context);
                }
                catch (Exception e)
                {
                    Log.Error("Exception in the request consumer loop:\n" + e);
                    
                    // this is a sign of a sandbox issue, restart
                    healthStateManager.SetUnhealthy();
                }
            }
        }

        public void Dispose()
        {
            loopCancellation?.Cancel();
            requestQueue?.PulseAll(); // needed, otherwise cancellation won't be noticed
            
            loopThread?.Join();

            loopCancellation?.Dispose();
            
            loopCancellation = null;
            loopThread = null;
        }

        private void HandleRequest(HttpListenerContext context)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            
            // initialize sandbox from header recipe
            if (!initializer.Initialized)
            {
                string recipeUrl = context.Request.Headers["X-Unisave-Initialization-Recipe-Url"];

                if (recipeUrl != null)
                    initializer.InitializeSandbox(recipeUrl).GetAwaiter().GetResult();
            }
        
            // read the request
            string executionParameters;
            using (var reader = new StreamReader(
                context.Request.InputStream,
                Encoding.UTF8
            ))
            {
                executionParameters = reader.ReadToEnd();
            }
            
            // perform the execution
            ExecutionResponse response = null;
            try
            {
                var request = new ExecutionRequest();
                request.ExecutionParameters = executionParameters;

                response = executionKernel.Handle(request);
            }
            catch (Exception e)
            {
                // report the issue also to the end user
                response = ExecutionResponse.SandboxException(e);
                
                Log.Error("Exception received from the execution kernel:\n" + e);
                    
                // this is a sign of a sandbox issue, restart
                healthStateManager.SetUnhealthy();
            }

            // send the response
            context.Response.StatusCode = 200;
            // TODO: set headers with timing and other stuff...
            Router.StringResponse(context, response.ExecutionResult, "application/json");
            
            // log
            sw.Stop();
            Log.Info(
                $"Handled request in {sw.ElapsedMilliseconds} ms, " +
                $"sent {context.Response.ContentLength64} bytes."
            );
        }
    }
}