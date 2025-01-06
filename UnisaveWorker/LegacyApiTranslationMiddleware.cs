using System;
using System.Collections.Generic;
using System.IO;
using System.Json;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace UnisaveWorker
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
    
    /// <summary>
    /// Translates legacy facet calling API to the new API both for request
    /// and for the response
    /// </summary>
    public class LegacyApiTranslationMiddleware
    {
        private readonly AppFunc next;
        
        public LegacyApiTranslationMiddleware(AppFunc next)
        {
            this.next = next;
        }
        
        public async Task Invoke(IDictionary<string, object> environment)
        {
            var context = new OwinContext(environment);
            
            await TranslateRequest(context);
            
            // intercept response body
            Stream actualResponseStream = context.Response.Body;
            var fakeResponseStream = new MemoryStream(10 * 1024); // 10 KB, grows
            context.Response.Body = fakeResponseStream;
            
            await next(environment);
            
            await TranslateResponse(
                context,
                actualResponseStream,
                fakeResponseStream
            );
        }

        private async Task TranslateRequest(IOwinContext context)
        {
            // parse request JSON
            string jsonString;
            using (var sr = new StreamReader(context.Request.Body, Encoding.UTF8))
                jsonString = await sr.ReadToEndAsync();
            var body = JsonValue.Parse(jsonString);

            // assert execution method
            if (body["method"] != "facet-call")
                throw new Exception("Only facet-call method is supported.");
            
            // parse out environment variables to be used for this request
            string environmentVariables = body["env"];
            context.Environment["worker.EnvString"] = environmentVariables;
            context.Environment["worker.EnvDict"] = ParseEnvVars(environmentVariables);

            // parse method parameters
            JsonObject methodParameters = (JsonObject)body["methodParameters"];
            string facetName = methodParameters["facetName"];
            string methodName = methodParameters["methodName"];
            JsonArray arguments = (JsonArray)methodParameters["arguments"];
            string sessionId = methodParameters["sessionId"];
            
            // translate request
            context.Request.Method = "POST";
            context.Request.Path = new PathString($"/{facetName}/{methodName}");
            context.Request.Headers["X-Unisave-Request"] = "Facet";
            context.Request.Headers["Content-Type"] = "application/json";
            if (sessionId != null)
            {
                context.Request.Headers["Cookie"] = "unisave_session_id=" +
                    Uri.EscapeDataString(sessionId) + ";";
            }
            
            JsonObject requestBody = new JsonObject() {
                ["arguments"] = arguments
            };
            byte[] requestBodyBytes = Encoding.UTF8.GetBytes(requestBody.ToString());
            context.Request.Body = new MemoryStream(requestBodyBytes, writable: false);
        }

        private async Task TranslateResponse(
            IOwinContext context,
            Stream actualResponseStream,
            MemoryStream fakeResponseStream
        )
        {
            context.Environment.TryGetValue(
                "worker.ExecutionDuration",
                out object executionDuration
            );
            
            // process the HTTP response
            if (context.Response.StatusCode != 200)
                throw new Exception(
                    "Response does not have 200 status"
                );
            int receivedBytes = int.Parse(
                context.Response.Headers["Content-Length"]
            );
            string newSessionId = ExtractSessionIdFromCookies(context.Response);
            JsonObject owinResponse = (JsonObject) JsonValue.Parse(
                await new StreamReader(
                    new MemoryStream(
                        fakeResponseStream.GetBuffer(), 0,
                        receivedBytes, writable: false
                    )
                ).ReadToEndAsync()
            );
            
            // convert response to entrypoint result
            JsonObject result = new JsonObject() {
                ["result"] = owinResponse["status"],
                ["special"] = new JsonObject() {
                    ["sessionId"] = newSessionId,
                    ["logs"] = owinResponse["logs"],
                    ["executionDuration"] = executionDuration as double? ?? 0.0
                }
            };
            
            if (result["result"] == "ok")
                result["returned"] = owinResponse["returned"];
                
            if (result["result"] == "exception")
                result["exception"] = owinResponse["exception"];

            // clean up response headers and return the correct body stream
            context.Response.Headers.Remove("Content-Type");
            context.Response.Headers.Remove("Set-Cookie");
            context.Response.Body = actualResponseStream;
            
            // send the actual response
            await context.SendResponse(
                statusCode: 200,
                body: result.ToString(),
                contentType: "application/json"
            );
        }
        
        /// <summary>
        /// Extracts session ID from Set-Cookie headers and
        /// returns null if that fails.
        /// </summary>
        private static string ExtractSessionIdFromCookies(IOwinResponse response)
        {
            const string prefix = "unisave_session_id=";
            
            IList<string> setCookies = response.Headers.GetValues("Set-Cookie");

            string sessionCookie = setCookies?.FirstOrDefault(
                c => c.Contains(prefix)
            );

            sessionCookie = sessionCookie?.Split(';')?.FirstOrDefault(
                c => c.StartsWith(prefix)
            );

            string sessionId = sessionCookie?.Substring(prefix.Length);

            if (sessionId == null)
                return null;
            
            return Uri.UnescapeDataString(sessionId);
        }

        private static Dictionary<string, string> ParseEnvVars(
            string source
        )
        {
            Dictionary<string, string> env = new Dictionary<string, string>();
            
            string[] lines = Regex.Split(source, "\r\n|\r|\n");
            
            foreach (string line in lines)
            {
                string[] parts = line.Split('=');

                if (parts.Length <= 1)
                    continue;

                string keyPart = parts[0];
                string valuePart = line.Substring(keyPart.Length + 1);
                string key = keyPart.Trim();
                string value = valuePart.Trim();

                if (key.StartsWith("#"))
                    continue;

                env[key] = value;
            }

            return env;
        }
    }
}