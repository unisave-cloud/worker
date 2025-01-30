using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Owin.Builder;
using Owin;
using UnisaveWorker;

namespace DotnetUnisaveWorker
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
    
    public class AspNetCoreHttpServerStarter : IHttpServerStarter, IDisposable
    {
        private readonly WebApplicationBuilder aspBuilder;
        private readonly WebApplication aspApp;
        
        private readonly CancellationTokenSource onAppDisposingCts = new();
        
        public AspNetCoreHttpServerStarter(string[] args)
        {
            // create the ASP.NET Core app
            aspBuilder = WebApplication.CreateBuilder(
                new WebApplicationOptions {
                    Args = args,
                    ApplicationName = typeof(Program).Assembly.FullName,
                    ContentRootPath = Path.GetDirectoryName(
                        Environment.ProcessPath
                    ) 
                }
            );
            aspApp = aspBuilder.Build();
        }
        
        public IDisposable Start(string url, Action<IAppBuilder> startup)
        {
            // === configure ===
            
            // prepare the OWIN startup properties dictionary
            IAppBuilder owinAppBuilder = new AppBuilder();
            owinAppBuilder.Properties["owin.Version"] = "1.0";
            owinAppBuilder.Properties["host.OnAppDisposing"]
                = onAppDisposingCts.Token;
            
            // build the OWIN app function
            startup.Invoke(owinAppBuilder);
            AppFunc owinAppFunc = owinAppBuilder.Build<AppFunc>();
            
            // bind the app function to the ASP.NET Core server
            aspApp.UseOwin(pipeline => {
                pipeline(next => owinAppFunc);
            });
            
            // set up the ASP.NET Core server parameters
            aspApp.Urls.Clear();
            aspApp.Urls.Add(url);
            
            // === start ===
            
            aspApp.StartAsync().GetAwaiter().GetResult();
            
            Log.Info("Started ASP.NET Core Kestrel Server.");
            
            // For stopping, an IDisposable object must be returned.
            // We return ourselves and handle stopping in the Dispose method.
            return this;
        }

        public void WaitForTermination()
        {
            aspApp.WaitForShutdownAsync().GetAwaiter().GetResult();
        }

        public void Dispose()
        {
            // trigger the OWIN "host.OnAppDisposing" token
            onAppDisposingCts.Cancel();
            
            aspApp.DisposeAsync().GetAwaiter().GetResult();
            
            onAppDisposingCts.Dispose();
        }
    }
}