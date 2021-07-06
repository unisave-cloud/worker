using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace UnisaveSandbox
{
    public class Initializer
    {
        private const string ExpectedRecipeHeader = "UNISAVE_SANDBOX_RECIPE v1";
        
        private readonly HttpClient http;
        
        public Initializer(HttpClient http)
        {
            this.http = http;
        }

        public async Task InitializeSandbox(string recipeUrl)
        {
            var response = await http.GetAsync(recipeUrl);

            response.EnsureSuccessStatusCode();

            using (var stream = await response.Content.ReadAsStreamAsync())
            {
                await ImplementRecipe(new StreamReader(stream));
            }
        }

        private async Task ImplementRecipe(StreamReader sr)
        {
            // === read the header ===
            
            string headerLine = await sr.ReadLineAsync();
            
            if (headerLine == null)
                throw new Exception("Given recipe is an empty file");
            
            if (headerLine != ExpectedRecipeHeader)
                throw new Exception("Invalid recipe header: " + headerLine);
            
            // === read individual records ===

            string path = null;
            
            while (true)
            {
                string line = await sr.ReadLineAsync();

                if (line == null)
                    break;
                
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                
                if (path == null)
                {
                    path = line;
                }
                else
                {
                    await DownloadFile(path, line);
                    path = null;
                }
            }
            
            if (path != null)
                throw new Exception("Recipe ended unexpectedly");
            
            Log.Info("Sandbox initialized.");
        }

        private async Task DownloadFile(string path, string url)
        {
            Log.Info($"Downloading '{path}'...");
            
            var response = await http.GetAsync(url);
            
            response.EnsureSuccessStatusCode();

            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            using (var stream = new FileStream(path, FileMode.CreateNew))
            {
                await response.Content.CopyToAsync(stream);
            }
        }
    }
}