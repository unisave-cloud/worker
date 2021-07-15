using System;

namespace UnisaveSandbox
{
    public class Config
    {
        public int Port { get; set; } = 8080;

        public string InitializationRecipeUrl { get; set; }

        /// <summary>
        /// Loads sandbox configuration from environment variables
        /// </summary>
        /// <returns></returns>
        public static Config LoadFromEnv()
        {
            var d = new Config();
            
            return new Config {
                Port = GetEnvInteger("SANDBOX_SERVER_PORT", d.Port),
                InitializationRecipeUrl = GetEnvString("INITIALIZATION_RECIPE_URL")
            };
        }

        private static int GetEnvInteger(string key, int defaultValue = 0)
        {
            string s = GetEnvString(key);

            if (string.IsNullOrEmpty(s))
                return defaultValue;
            
            if (int.TryParse(s, out int i))
                return i;

            return defaultValue;
        }

        private static string GetEnvString(string key, string defaultValue = null)
        {
            string v = Environment.GetEnvironmentVariable(key);
            
            if (string.IsNullOrEmpty(v))
                v = defaultValue;
            
            return v;
        }
    }
}