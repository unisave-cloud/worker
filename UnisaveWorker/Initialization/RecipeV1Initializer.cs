using System;
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
        
        public RecipeV1Initializer(HttpClient http)
        {
            this.http = http;
        }

        protected override Task PerformInitialization(
            string recipeUrl,
            CancellationToken cancellationToken
        )
        {
            throw new NotImplementedException();
        }
    }
}