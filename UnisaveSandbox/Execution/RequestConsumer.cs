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

        private CancellationTokenSource loopCancellation;
        private Thread loopThread;

        public RequestConsumer(RequestQueue requestQueue, Initializer initializer)
        {
            this.requestQueue = requestQueue;
            this.initializer = initializer;
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
                    Log.Error(
                        "An exception occured when " +
                        "processing an execution request:\n" + e
                    );
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
            var executor = new Executor();
            string executionResponse = executor.ExecuteBackend(
                executionParameters
            );
            
            // send the response
            context.Response.StatusCode = 200;
            Router.StringResponse(context, executionResponse, "application/json");
            
            // log
            sw.Stop();
            Log.Info(
                $"Handled request in {sw.ElapsedMilliseconds} ms, " +
                $"sent {context.Response.ContentLength64} bytes."
            );
        }
    }
}