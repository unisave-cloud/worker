using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Watchdog.Http
{
    public class HttpServer : IDisposable
    {
        private readonly HttpListener listener;
        private readonly Router router;

        private Task listeningLoopTask;

        private bool alreadyDisposed;

        private readonly bool verbose;
        
        public HttpServer(int port, bool verbose, Router router)
        {
            listener = new HttpListener();
            listener.Prefixes.Add("http://*:" + port + "/");

            this.router = router;
            this.verbose = verbose;
        }
 
        public void Start()
        {
            if (alreadyDisposed)
                throw new ObjectDisposedException(
                    "Http server has been already stopped."
                );
            
            listener.Start();

            // start listening in a new task
            listeningLoopTask = ListeningLoopAsync();
        }

        private async Task ListeningLoopAsync()
        {
            // WARNING: access only from within this method
            // to make sure no race condition occurs
            List<Task> pendingRequests = new List<Task>();
            
            while (listener.IsListening)
            {
                HttpListenerContext ctx;
                
                // wait for a request
                try
                {
                    ctx = await listener.GetContextAsync();
                }
                catch (ObjectDisposedException)
                {
                    // listener has been stopped, break the loop
                    break;
                }
                
                pendingRequests.Add(
                    HandleConnectionAsync(ctx)
                );
                
                // clean up finished tasks
                pendingRequests.RemoveAll(t => {
                    if (!t.IsCompleted)
                        return false;

                    // process any exceptions before removing the task
                    t.Wait();
                    return true;
                });
            }
            
            // wait for all pending requests to finish
            await Task.WhenAll(pendingRequests);
        }

        private async Task HandleConnectionAsync(HttpListenerContext context)
        {
            // NOTE: this method should not really throw an exception
            // if it does, it will take down the entire server
            
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // make sure we *will* run asynchronously
            await Task.Yield();

            try
            {
                if (verbose)
                    PrintVerboseRequestInformation(context);
                
                await router.HandleRequestAsync(context);
            }
            catch (Exception e)
            {
                Log.Error(
                    "An exception occured when " +
                    "processing an HTTP request:\n" + e
                );
            }
            
            // NOTE: Do not close the response stream here, as some requests
            // are handled asynchronously by other threads and so the request
            // may still be processed.
        }

        private void PrintVerboseRequestInformation(HttpListenerContext context)
        {
            var r = context.Request;
            
            StringBuilder sb = new StringBuilder();
            
            sb.AppendLine(
                $"{r.HttpMethod} {r.Url.PathAndQuery} HTTP/{r.ProtocolVersion}"
            );
            
            foreach (var key in r.Headers.AllKeys)
                sb.AppendLine($"{key}: {r.Headers[key]}");
            
            Log.Debug("HTTP Server received request with this head:\n" + sb);
        }
 
        public void Stop()
        {
            if (alreadyDisposed)
                return;
            
            listener.Stop();
            listener.Close();

            listeningLoopTask?.Wait();
            listeningLoopTask = null;

            alreadyDisposed = true;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}