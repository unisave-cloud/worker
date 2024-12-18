using System;
using Mono.Unix;
using Mono.Unix.Native;
using Watchdog;

namespace UnisaveWorker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // TODO: owin http listener
            // https://github.com/evicertia/Katana/blob/master/src/Microsoft.Owin.Host.HttpListener/OwinHttpListener.cs
            
            // TODO: overview of async-await in .NET
            // https://devblogs.microsoft.com/dotnet/configureawait-faq/
            
            // TODO: limiting thread-concurrency via a task scheduler
            // https://stackoverflow.com/questions/69222176/run-valuetasks-on-a-custom-thread-pool
            
            Config config = Config.LoadFromEnv();
            
            using (var app = new WorkerApplication(config))
            {
                app.Start();
                
                WaitForTermination();
            }
            
            // if there was an execution timeout
            // or the user started some rogue threads,
            // this kills all of them:
            Environment.Exit(0);
            
            // TODO: playing around with JSON, useful for ENV parsing
            // System.Json namespace docs:
            // https://learn.microsoft.com/en-us/dotnet/api/system.json?view=netframework-4.7.2
            // JsonValue x = JsonValue.Parse("{'foo': 'bar'}".Replace("'", "\""));
            // Console.WriteLine((string)x["foo"]);
        }

        private static void WaitForTermination()
        {
            if (IsRunningOnMono())
            {
                UnixSignal.WaitAny(GetUnixTerminationSignals());
            }
            else
            {
                Console.WriteLine("Press enter to stop the application.");
                Console.ReadLine();
            }
        }
        
        private static bool IsRunningOnMono()
        {
            return Type.GetType("Mono.Runtime") != null;
        }

        private static UnixSignal[] GetUnixTerminationSignals()
        {
            return new[]
            {
                new UnixSignal(Signum.SIGINT),
                new UnixSignal(Signum.SIGTERM),
                new UnixSignal(Signum.SIGQUIT),
                new UnixSignal(Signum.SIGHUP)
            };
        }
    }
}