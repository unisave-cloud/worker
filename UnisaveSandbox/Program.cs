using System;
using Mono.Unix;
using Mono.Unix.Native;

namespace UnisaveSandbox
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Config config = Config.LoadFromEnv();
            
            using (var server = new SandboxServer(config))
            {
                server.Start();
                
                WaitForTermination();
            }
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