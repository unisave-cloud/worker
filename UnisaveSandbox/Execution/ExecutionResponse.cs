using System;
using System.Reflection;

namespace UnisaveSandbox.Execution
{
    public class ExecutionResponse
    {
        /// <summary>
        /// The JSON string that unisave framework returned
        /// </summary>
        public string ExecutionResult { get; set; }
        
        // TODO: request timing, network usage, memory usage, etc...

        /// <summary>
        /// Sandbox crashed, turn it into a response
        /// </summary>
        public static ExecutionResponse SandboxException(Exception e)
        {
            string version = typeof(ExecutionResponse).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                .InformationalVersion;

            string message =
                $"Unisave experienced an internal error, " +
                $"please send this log to developers of Unisave.\n" +
                $"The exception occured in sandbox, version {version}\n" +
                $"The exception:\n{e}";

            string messageEscaped = message
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n");
            
            string jsonResponse = @"{
                'result': 'exception',
                'exception': {
                    'ClassName': 'System.Exception',
                    'Message': '###',
                    'StackTraceString': '   at UnisaveSandbox'
                },
                'special': {}
            }".Replace('\'', '\"').Replace("###", messageEscaped);
            
            return new ExecutionResponse {
                ExecutionResult = jsonResponse
            };
        }
    }
}