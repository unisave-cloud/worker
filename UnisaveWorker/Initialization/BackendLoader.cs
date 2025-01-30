using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Owin;

namespace UnisaveWorker.Initialization
{
    /// <summary>
    /// Service that encapsulates loading of game backend assemblies
    /// </summary>
    public class BackendLoader
    {
        /// <summary>
        /// Returns all assemblies that belong to the game's backend
        /// (including the unisave framework). Corresponds to the
        /// OWIN `unisave.GameAssemblies` property.
        /// </summary>
        public IReadOnlyList<Assembly> GameAssemblies
            => gameAssemblies ?? throw new BackendNotLoadedException();
        private List<Assembly>? gameAssemblies = null;
        
        /// <summary>
        /// References the backend.dll file
        /// </summary>
        public Assembly LegacyGameAssembly
            => legacyGameAssembly ?? throw new BackendNotLoadedException();
        private Assembly? legacyGameAssembly = null;
        
        /// <summary>
        /// References the UnisaveFramework.dll file
        /// </summary>
        public Assembly LegacyFrameworkAssembly
            => legacyFrameworkAssembly ?? throw new BackendNotLoadedException();
        private Assembly? legacyFrameworkAssembly = null;

        /// <summary>
        /// The "Startup" class in OWIN that creates the backend AppFunc
        /// </summary>
        public MethodInfo OwinStartupConfigurationMethod
            => owinStartupConfigurationMethod ?? throw new BackendNotLoadedException();
        private MethodInfo? owinStartupConfigurationMethod = null;
        
        /// <summary>
        /// The "Unisave.Runtime.Entrypoint.Start()" method used to call
        /// Unisave Framework before the v0.11.0 version
        /// </summary>
        public MethodInfo LegacyStartMethod
            => legacyStartMethod ?? throw new BackendNotLoadedException();
        private MethodInfo? legacyStartMethod = null;
        
        public bool HasOwinStartupConfigurationMethod
            => owinStartupConfigurationMethod != null;
        public bool HasLegacyStartMethod => legacyStartMethod != null;

        /// <summary>
        /// The version of the Unisave Framework used by the backend code
        /// (null if not found, the game maybe does not use Unisave Framework)
        /// </summary>
        public Version? UnisaveFrameworkVersion { get; private set; } = null;

        /// <summary>
        /// Friendly name of the OwinStartupAttribute
        /// </summary>
        private readonly string owinStartupAttributeName;
        
        public BackendLoader(string owinStartupAttributeName)
        {
            this.owinStartupAttributeName = owinStartupAttributeName;
        }
        
        public void Load(string backendFolderPath)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            
            LoadAssemblies(backendFolderPath);
            
            FindOwinStartupConfigurationMethod();
            FindLegacyStartMethod();

            FindFrameworkVersion();
            
            stopwatch.Stop();
            Log.Info(
                $"Loaded {gameAssemblies?.Count} game assemblies " +
                $"in {stopwatch.ElapsedMilliseconds}ms."
            );

            if (!HasOwinStartupConfigurationMethod && !HasLegacyStartMethod)
                throw new Exception(
                    "Neither the OWIN startup class, nor the legacy " +
                    "entrypoint method was found."
                );
        }
        
        private void LoadAssemblies(string backendFolderPath)
        {
            List<Assembly> assemblies = new List<Assembly>();
            Assembly? legacyGameAsm = null;
            Assembly? legacyFrameworkAsm = null;
            
            IEnumerable<string> filePaths = Directory.EnumerateFiles(
                backendFolderPath, "*", SearchOption.AllDirectories
            ).OrderBy(x => x);

            foreach (string filePath in filePaths)
            {
                // skip non-dll files
                if (Path.GetExtension(filePath)?.ToLowerInvariant() != ".dll")
                    continue;
                
                // load the assembly
                Assembly assembly = Assembly.LoadFrom(filePath);
                
                // remember all assemblies
                assemblies.Add(assembly);
                
                string fileName = Path.GetFileName(filePath);
                
                // remember game assembly
                if (fileName == "backend.dll")
                    legacyGameAsm = assembly;
                
                // remember framework assembly
                if (fileName == "UnisaveFramework.dll")
                    legacyFrameworkAsm = assembly;
            }
            
            // update loader state
            gameAssemblies = assemblies;
            legacyGameAssembly = legacyGameAsm;
            legacyFrameworkAssembly = legacyFrameworkAsm;
        }

        private void FindOwinStartupConfigurationMethod()
        {
            OwinStartupAttribute? attribute = FindOwinStartupAttribute();
            
            if (attribute == null)
                return;

            string methodName = "Configuration";
            if (!string.IsNullOrEmpty(attribute.MethodName))
                methodName = attribute.MethodName;

            // get the method
            // (may result in null, which is fine)
            owinStartupConfigurationMethod = attribute.StartupType
                ?.GetMethod(methodName);
        }

        private OwinStartupAttribute? FindOwinStartupAttribute()
        {
            foreach (Assembly assembly in GameAssemblies)
            foreach (
                OwinStartupAttribute attribute
                in assembly.GetCustomAttributes<OwinStartupAttribute>()
            )
            {
                // take only attributes that are labeled "UnisaveFramework"
                // (unless configured otherwise)
                if (attribute.FriendlyName != owinStartupAttributeName)
                    continue;
                
                // we will handle the first attribute that matches our search
                return attribute;
            }

            return null;
        }

        private void FindLegacyStartMethod()
        {
            // may result in null, which is fine
            legacyStartMethod = legacyFrameworkAssembly
                ?.GetType("Unisave.Runtime.Entrypoint")
                ?.GetMethod("Start");
        }

        private void FindFrameworkVersion()
        {
            if (legacyFrameworkAssembly == null)
            {
                UnisaveFrameworkVersion = null;
                return;
            }
            
            AssemblyName name = legacyFrameworkAssembly.GetName();
            UnisaveFrameworkVersion = name.Version;
        }
    }
}