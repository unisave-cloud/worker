using System;
using UnisaveWorker;

namespace DotnetUnisaveWorker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Config config = Config.LoadFromEnv();
            var starter = new AspNetCoreHttpServerStarter(args);
            
            using (var app = new WorkerApplication(config, starter))
            {
                app.Start();
                
                starter.WaitForTermination();
                
                app.Stop();
            }
            
            // if there was an execution timeout
            // or the user started some rogue threads,
            // this kills all of them:
            Environment.Exit(0);
        }
    }
}