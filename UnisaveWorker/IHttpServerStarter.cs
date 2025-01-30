using System;
using Owin;

namespace UnisaveWorker
{
    /// <summary>
    /// Starts the OWIN Katana or ASP.NET Core HTTP server,
    /// depending on the chosen runtime.
    /// </summary>
    public interface IHttpServerStarter
    {
        /// <summary>
        /// Analogous to the OWIN Katana 'WebApp.Start' method.
        /// </summary>
        /// <param name="url">
        /// What URL should the HTTP server listen at
        /// </param>
        /// <param name="startup">
        /// The OWIN startup class's configure method (builds the OWIN pipeline)
        /// </param>
        /// <returns>
        /// The HTTP server that, when disposed, stops listening.
        /// </returns>
        IDisposable Start(string url, Action<IAppBuilder> startup);
    }
}