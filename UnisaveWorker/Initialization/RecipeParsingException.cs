using System;

namespace UnisaveWorker.Initialization
{
    /// <summary>
    /// Thrown by the code that parses the initialization recipe
    /// </summary>
    public class RecipeParsingException : Exception
    {
        public RecipeParsingException()
        {
        }

        public RecipeParsingException(string message) : base(message)
        {
        }

        public RecipeParsingException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}