using System;
using System.Collections.Generic;
using System.IO;
using System.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin;
using UnisaveWorker.Initialization;

namespace UnisaveWorker.Execution
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
    
    /// <summary>
    /// Executes game backend via the legacy static Entrypoint class
    /// </summary>
    public class LegacyEntrypointExecutionMiddleware
    {
        private readonly BackendLoader backendLoader;
        
        private bool warningLogged = false;

        public LegacyEntrypointExecutionMiddleware(
            AppFunc next, // not used
            BackendLoader backendLoader
        )
        {
            this.backendLoader = backendLoader;
        }

        public async Task Invoke(IDictionary<string, object> environment)
        {
            LogLegacyEntrypointUsageWarning();
            
            var context = new OwinContext(environment);
            
            string executionResult = (string) backendLoader.LegacyStartMethod
                .Invoke(
                    null,
                    new object[] {
                        await ConstructExecutionParameters(context),
                        backendLoader.LegacyGameAssembly.GetTypes()
                    }
                );
            
            await ProcessExecutionResult(
                (JsonObject) JsonValue.Parse(executionResult),
                context
            );
        }

        private void LogLegacyEntrypointUsageWarning()
        {
            if (warningLogged)
                return;
            
            warningLogged = true;
            
            Log.Warning(
                "Using the legacy entrypoint for backend execution."
            );
        }

        private async Task<string> ConstructExecutionParameters(
            IOwinContext context
        )
        {
            string envString = context.Environment["worker.EnvString"] as string
                ?? throw new Exception("Missing 'worker.EnvString' value.");
            
            string[] pathSegments = context.Request.Path.Value.Split('/');
            if (pathSegments.Length != 3)
                throw new Exception("Invalid number of path segments.");

            string? sessionId = context.Request.Cookies["unisave_session_id"];
            
            // WARNING: Nulls in strings in System.Json are messed up.
            // Converting (string?)null to JsonValue? creates an empty string,
            // instead of (JsonValue?)null value.
            JsonValue? sessionIdAsJson = sessionId == null
                // ReSharper disable once RedundantCast
                ? (JsonValue?)null // creates a true JSON null
                // ReSharper disable once RedundantCast
                : (JsonValue?)sessionId; // values work, nulls breaks

            using var streamReader = new StreamReader(
                context.Request.Body,
                Encoding.UTF8
            );
            string requestBodyString = await streamReader.ReadToEndAsync();
            JsonObject requestBody = (JsonObject) JsonValue.Parse(
                requestBodyString
            );

            var executionParameters = new JsonObject {
                ["env"] = envString,
                ["method"] = "facet-call",
                ["methodParameters"] = new JsonObject
                {
                    ["facetName"] = pathSegments[1],
                    ["methodName"] = pathSegments[2],
                    ["arguments"] = requestBody["arguments"],
                    ["sessionId"] = sessionIdAsJson
                }
            };

            return executionParameters.ToString();
        }

        private async Task ProcessExecutionResult(
            JsonObject executionResult,
            IOwinContext context
        )
        {
            string? newSessionId = executionResult["special"]["sessionId"];
            if (!string.IsNullOrEmpty(newSessionId))
            {
                context.Response.Cookies.Append(
                    "unisave_session_id",
                    newSessionId,
                    new CookieOptions() {
                        HttpOnly = true,
                        Path = "/",
                        Expires = DateTime.UtcNow.Add(
                            TimeSpan.FromSeconds(3_600) // 1h
                        )
                    }
                );
            }

            // === body ===
            
            JsonObject owinResponse = new JsonObject {
                ["status"] = executionResult["result"],
                ["logs"] = executionResult["special"]["logs"]
            };

            if (owinResponse["status"] == "ok")
            {
                owinResponse["returned"] = executionResult["returned"];
            }

            if (owinResponse["status"] == "exception")
            {
                owinResponse["exception"] = executionResult["exception"];
                owinResponse["isKnownException"] = true;
            }

            await context.SendResponse(
                statusCode: 200,
                body: owinResponse.ToString(),
                contentType: "application/json"
            );
        }
    }
}