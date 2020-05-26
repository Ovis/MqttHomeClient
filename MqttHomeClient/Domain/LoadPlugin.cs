using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;
using PluginInterface;
using ZLogger;

namespace MqttHomeClient.Domain
{
    public class LoadPlugin
    {
        private readonly ILogger<LoadPlugin> _logger;

        public LoadPlugin(ILogger<LoadPlugin> logger)
        {
            _logger = logger;
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

            var pluginTypes = new List<Type>();

            foreach (var dll in availableDllList)
            {
                try
                {
                    var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(dll);

                    var types = assembly.GetTypes();

                    //IPluginインターフェイスで実装されたプラグインのみをロード
                    pluginTypes.AddRange(types.Where(type => !type.IsInterface && !type.IsAbstract).Where(type => type.GetInterface(typeof(IPlugin).FullName!) != null));
                }
                catch (Exception e)
                {
                    _logger.ZLogError(e.Message);
                }

            }

            plugins.AddRange(pluginTypes.Select(pType => (IPlugin)Activator.CreateInstance(pType)));

            return plugins;

        }
    }
}