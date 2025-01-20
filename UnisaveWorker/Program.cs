using System;
using Mono.Unix;
using Mono.Unix.Native;

namespace UnisaveWorker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Config config = Config.LoadFromEnv();
            
            using (var app = new WorkerApplication(config))
            {
                app.Start();
                
                WaitForTermination();

                app.Stop();
            }
            
            // if there was an execution timeout
            // or the user started some rogue threads,
            // this kills all of them:
            Environment.Exit(0);
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