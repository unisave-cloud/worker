using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace UnisaveSandbox
{
    /// <summary>
    /// Manages sandbox initialization
    /// </summary>
    public class Initializer
    {
        private const string ExpectedRecipeHeader = "UNISAVE_SANDBOX_RECIPE v1";
        
        private readonly HttpClient http;

        /// <summary>
        /// Has this sandbox been initialized already?
        /// </summary>
        public bool Initialized { get; private set; } = false;
        
        public Initializer(HttpClient http)
        {
            this.http = http;
        }

        public void DummyInitialization()
        {
            Log.Warning("Starting dummy initialization...");
            
            File.Copy("/dummy/DummyFramework.dll", "UnisaveFramework.dll");
            File.Copy("/dummy/DummyFramework.pdb", "UnisaveFramework.pdb");
            File.Copy("/dummy/DummyGame.dll", "backend.dll");
            File.Copy("/dummy/DummyGame.pdb", "backend.pdb");
            
            Initialized = true;
            
            Log.Warning("Sandbox dummy-initialized.");
        }

        public async Task InitializeSandbox(string recipeUrl)
        {
            if (Initialized)
                throw new InvalidOperationException(
                    "The sandbox was already initialized."
                );
            
            Log.Info("Starting initialization...");

            var response = await http.GetAsync(recipeUrl);

            response.EnsureSuccessStatusCode();

            using (var stream = await response.Content.ReadAsStreamAsync())
            {
                await ImplementRecipe(new StreamReader(stream));
            }
            
            Initialized = true;
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