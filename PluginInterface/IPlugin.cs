using System.Threading.Tasks;

namespace PluginInterface
{
    public interface IPlugin
    {
        string Topic { get; }

        string PluginName { get; }

        bool Action(string text);

        Task<bool> ActionAsync(string text);
    }
}
