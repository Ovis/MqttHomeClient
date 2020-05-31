using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;
using PluginInterface;
using ZLogger;

namespace MqttHomeClient.Domain
{
    public class LoadPlugin : AssemblyLoadContext
    {
        private readonly ILogger<LoadPlugin> _logger;

        private AssemblyDependencyResolver _resolver;

        public LoadPlugin(ILogger<LoadPlugin> logger)
        {
            _logger = logger;

            var pluginPath = Path.Combine(Directory.GetCurrentDirectory(), "Plugins");
            _resolver = new AssemblyDependencyResolver(pluginPath);
        }


        protected override Assembly Load(AssemblyName assemblyName)
        {
            string assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            return null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            string libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (libraryPath != null)
            {
                return LoadUnmanagedDllFromPath(libraryPath);
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// プラグインをロード
        /// </summary>
        /// <returns></returns>
        public List<IPlugin> LoadPlugins()
        {
            var plugins = new List<IPlugin>();

            var path = Path.Combine(Directory.GetCurrentDirectory(), "Plugins");

            if (!Directory.Exists(path)) return plugins;

            var availableDllList = Directory.GetFiles(path, "*.dll").Select(Path.GetFullPath).ToArray();

            return availableDllList.SelectMany(pluginPath =>
             {
                 Assembly pluginAssembly = LoadAllPlugin(pluginPath);
                 return CreateCommands(pluginAssembly);
             }).ToList();

        }


        private Assembly LoadAllPlugin(string pluginPath)
        {
            Console.WriteLine($"Loading commands from: {pluginPath}");
            var loadContext = new PluginLoadContext(pluginPath);
            return loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(pluginPath)));
        }


        private IEnumerable<IPlugin> CreateCommands(Assembly assembly)
        {
            var count = 0;

            foreach (var type in assembly.GetTypes())
            {
                if (typeof(IPlugin).IsAssignableFrom(type))
                {
                    if (Activator.CreateInstance(type) is IPlugin result)
                    {
                        count++;
                        yield return result;
                    }
                }
            }

            if (count == 0)
            {
                _logger.ZLogWarning($"DLL read error. assembly:{assembly}");
            }
        }
    }
}