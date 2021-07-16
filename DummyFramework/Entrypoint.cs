using System;
using System.Linq;
using System.Threading;

// ReSharper disable once CheckNamespace
namespace Unisave.Runtime
{
    public static class Entrypoint
    {
        public static string Start(
            string executionParametersAsJson,
            Type[] gameAssemblyTypes
        )
        {
            if (executionParametersAsJson == null)
                return "DummyFramework: Execution parameters are null\n";
            
            if (gameAssemblyTypes == null)
                return "DummyFramework: Game assembly types are null\n";
            
            switch (executionParametersAsJson.Trim())
            {
                case "sleep 1":
                    Thread.Sleep(1000);
                    return "DummyFramework: Slept for 1 second\n";
                
                case "sleep 5":
                    Thread.Sleep(5000);
                    return "DummyFramework: Slept for 5 seconds\n";
                
                case "sleep 15":
                    Thread.Sleep(15000);
                    return "DummyFramework: Slept for 15 seconds\n";
                
                case "sleep 30":
                    Thread.Sleep(30000);
                    return "DummyFramework: Slept for 30 seconds\n";
                
                case "sleep 60":
                    Thread.Sleep(60000);
                    return "DummyFramework: Slept for 60 seconds\n";
                
                case "game assembly":
                    return "DummyFramework: Game assembly types:\n" + string.Concat(
                        gameAssemblyTypes.Select(t => t.FullName + "\n")
                    );
                
                case "exit":
                    Environment.Exit(0);
                    return "DummyFramework: Exited\n"; // this won't be received
                
                default:
                    return "DummyFramework: Did run!\n";
            }
        }
    }
}