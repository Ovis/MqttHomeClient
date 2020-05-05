using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using PluginInterface;

namespace MqttHomeClient.Domain
{
    public static class LoadPlugin
    {
        /// <summary>
        /// プラグインをロード
        /// </summary>
        /// <returns></returns>
        public static List<IPlugin> LoadPlugins()
        {
            var plugins = new List<IPlugin>();

            var path = Path.Combine(Directory.GetCurrentDirectory(), "Plugins");

            if (!Directory.Exists(path)) return plugins;

            var availableDllList = Directory.GetFiles(path, "*.dll").Select(s => Path.GetFullPath(s)).ToArray();

            var pluginTypes = new List<Type>();

            foreach (var dll in availableDllList)
            {
                var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(dll);

                var types = assembly.GetTypes();

                //IPluginインターフェイスで実装されたプラグインのみをロード
                pluginTypes.AddRange(types.Where(type => !type.IsInterface && !type.IsAbstract).Where(type => type.GetInterface(typeof(IPlugin).FullName!) != null));
            }

            plugins.AddRange(pluginTypes.Select(pType => (IPlugin)Activator.CreateInstance(pType)));

            return plugins;

        }
    }
}