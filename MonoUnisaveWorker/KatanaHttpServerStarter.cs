using System;
using Microsoft.Owin.Hosting;
using Owin;
using UnisaveWorker;

namespace MonoUnisaveWorker
{
    public class KatanaHttpServerStarter : IHttpServerStarter
    {
        public IDisposable Start(string url, Action<IAppBuilder> startup)
        {
            var server = WebApp.Start(
                url: url, // e.g. "http://*:8080"
                startup: startup
            );
            
            Log.Info("Started OWIN Katana Server.");

            return server;
        }
    }
}