using System;
using System.IO;
using System.Reflection;

namespace Watchdog.Execution
{
    public class Executor
    {
        private Assembly gameAssembly;
        private Assembly frameworkAssembly;
        
        /// <summary>
        /// Executes the game backend
        /// (directly calls the unisave framework entrypoint)
        /// </summary>
        /// <param name="executionParameters"></param>
        /// <returns></returns>
        public string ExecuteBackend(string executionParameters)
        {
            try
            {
                LoadAssemblies();

                MethodInfo frameworkStartMethod = frameworkAssembly
                    .GetType("Unisave.Runtime.Entrypoint")
                    .GetMethod("Start");

                if (frameworkStartMethod == null)
                    throw new NullReferenceException(
                        "Framework entrypoint is missing the Start method."
                    );

                string executionResult = (string) frameworkStartMethod.Invoke(
                    null,
                    new object[] {
                        executionParameters,
                        gameAssembly.GetTypes()
                    }
                );

                return executionResult;
            }
            catch (Exception e)
            {
                // will show up on the client as a regular exception
                return FormatWorkerException(e);
            }
        }

        private void LoadAssemblies()
        {
            var filePaths = Directory.EnumerateFiles(
                "./", "*", SearchOption.AllDirectories
            );

            foreach (string filePath in filePaths)
            {
                // skip non-dll files
                if (Path.GetExtension(filePath)?.ToLowerInvariant() != ".dll")
                    continue;
                
                // load the assembly
                var asm = Assembly.LoadFile(
                    Path.GetFullPath(filePath) // needs to be an absolute path
                );
                
                // remember game assembly
                if (filePath == "./backend.dll")
                    gameAssembly = asm;
                
                // remember framework assembly
                if (filePath == "./UnisaveFramework.dll")
                    frameworkAssembly = asm;
            }
            
            if (gameAssembly == null)
                throw new NullReferenceException(
                    "Game assembly hasn't been loaded"
                );
            
            if (frameworkAssembly == null)
                throw new NullReferenceException(
                    "Framework assembly hasn't been loaded"
                );
        }

        private string FormatWorkerException(Exception e)
        {
            string version = typeof(Executor).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                .InformationalVersion;

            string message =
                $"Unisave experienced an internal error, " +
                $"please send this log to developers of Unisave.\n" +
                $"The exception occured in worker, version {version}\n" +
                $"The exception:\n{e}";

            string messageEscaped = message
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n");
            
            return @"{
                'result': 'exception',
                'exception': {
                    'ClassName': 'System.Exception',
                    'Message': '###',
                    'StackTraceString': '   at UnisaveWorker'
                },
                'special': {}
            }".Replace('\'', '\"').Replace("###", messageEscaped);
        }
    }
}