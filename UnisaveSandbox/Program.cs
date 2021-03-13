using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace UnisaveSandbox
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length <= 0)
            {
                PrintUsage();
                return;
            }

            switch (args[0])
            {
                case "init":
                    Initialize(args).GetAwaiter().GetResult();
                    break;
                
                case "exec":
                    Execute();
                    break;
                
                default:
                    PrintUsage();
                    break;
            }
        }

        private static async Task Initialize(string[] args)
        {
            if (args.Length != 2)
            {
                PrintUsage();
                return;
            }

            using (var http = new HttpClient())
            {
                var initializer = new Initializer(http);

                await initializer.InitializeSandbox(args[1]);
            }
        }

        private static void Execute()
        {
            // TODO: authenticate the request
            
            // receive function request
            string executionParameters = Console.In.ReadToEnd();

            // prepare response variable
            string executionResponse;
            
            using (var logFile = new FileStream("stdout.log", FileMode.Append))
            {
                // redirect standard output to a log file (append)
                Console.SetOut(new StreamWriter(logFile));

                // perform the execution
                var executor = new Executor();
                executionResponse = executor.ExecuteBackend(
                    executionParameters
                );
            }
                
            // recover standard output
            var stdout = new StreamWriter(
                Console.OpenStandardOutput()
            );
            stdout.AutoFlush = true;
            Console.SetOut(stdout);

            // send function response
            Console.Write(executionResponse);
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: sandbox.exe [command] [ARGUMENTS...]");
            Console.WriteLine("Commands:");
            Console.WriteLine("");
            Console.WriteLine("init [recipe-url]");
            Console.WriteLine("    Initializes the sandbox during startup");
            Console.WriteLine("");
            Console.WriteLine("exec");
            Console.WriteLine("    Executes a request on stdin in the " +
                              "initialized sandbox, printing output to stdout");
        }
    }
}