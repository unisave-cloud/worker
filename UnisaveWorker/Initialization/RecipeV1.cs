using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace UnisaveWorker.Initialization
{
    /// <summary>
    /// Holds a parsed v1 initialization recipe
    /// </summary>
    public class RecipeV1
    {
        // sandbox is a legacy name for watchdog/worker
        private const string ExpectedRecipeHeader = "UNISAVE_SANDBOX_RECIPE v1";
        
        public record RecipeRow(string Path, string Url)
        {
            public string Path { get; } = Path;
            public string Url { get; } = Url;
        }
        
        public List<RecipeRow> Rows { get; } = new List<RecipeRow>();
        
        public static async Task<RecipeV1> Parse(StreamReader reader)
        {
            var recipe = new RecipeV1();
            
            // === read the header ===
            
            string headerLine = await reader.ReadLineAsync();
            
            if (headerLine == null)
                throw new RecipeParsingException("Recipe is an empty file");
            
            if (headerLine != ExpectedRecipeHeader)
                throw new RecipeParsingException(
                    "Invalid recipe header: " + headerLine
                );
            
            // === read individual records ===

            string? path = null;
            
            while (true)
            {
                string line = await reader.ReadLineAsync();

                // end of file
                if (line == null)
                    break;
                
                // skip empty lines
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                
                // first comes a file path
                if (path == null)
                {
                    path = line;
                }
                else // then second a URL
                {
                    if (!Uri.IsWellFormedUriString(line, UriKind.Absolute))
                        throw new RecipeParsingException(
                            "Recipe URL is not a well formed URI string: " + line
                        );
                    
                    // record the parsed line
                    recipe.Rows.Add(new RecipeRow(path, line));
                    path = null;
                }
            }
            
            // path was parsed but not any URL, we expect a URL line now
            if (path != null)
                throw new RecipeParsingException("Recipe ended unexpectedly");
            
            return recipe;
        }
    }
}