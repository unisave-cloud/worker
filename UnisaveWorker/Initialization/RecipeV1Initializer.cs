using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace UnisaveWorker.Initialization
{
    /// <summary>
    /// Implements initialization from the v1 Unisave initialization recipe
    /// (see the documentation on initialization to learn more)
    /// </summary>
    public class RecipeV1Initializer : Initializer
    {
        private readonly HttpClient http;
        
        public RecipeV1Initializer(
            HttpClient http,
            string owinStartupAttributeName
        ) : base(owinStartupAttributeName)
        {
            this.http = http;
        }

        protected override async Task PerformInitialization(
            string recipeUrl,
            CancellationToken cancellationToken
        )
        {
            // download and parse the initialization recipe
            RecipeV1 recipe = await DownloadAndParseRecipe(
                recipeUrl, cancellationToken
            );

            foreach (RecipeV1.RecipeRow row in recipe.Rows)
            {
                await DownloadFile(
                    path: row.Path,
                    url: row.Url
                );
            }
        }

        private async Task<RecipeV1> DownloadAndParseRecipe(
            string recipeUrl,
            CancellationToken cancellationToken
        )
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            
            var response = await http.GetAsync(recipeUrl, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            stopwatch.Stop();
            Log.Info(
                $"Downloaded initialization recipe " +
                $"in {stopwatch.ElapsedMilliseconds}ms."
            );

            using var stream = await response.Content.ReadAsStreamAsync();
            return await RecipeV1.Parse(new StreamReader(stream));
        }
        
        private async Task DownloadFile(string path, string url)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            
            // download the file
            var response = await http.GetAsync(url);
            response.EnsureSuccessStatusCode();

            // create its parent directory
            string fullPath = Path.Combine(BackendFolderPath, path);
            string? directory = Path.GetDirectoryName(fullPath);

            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);
            else
                throw new Exception(
                    $"Cannot resolve parent directory for path: {fullPath}"
                );

            // write the file
            using var stream = new FileStream(fullPath, FileMode.CreateNew);
            await response.Content.CopyToAsync(stream);
            
            // log
            stopwatch.Stop();
            Log.Info(
                $"Downloaded '{path}' " +
                $"in {stopwatch.ElapsedMilliseconds}ms."
            );
        }
    }
}