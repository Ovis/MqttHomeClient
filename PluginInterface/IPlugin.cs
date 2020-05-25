using System.Threading.Tasks;

namespace PluginInterface
{
    public interface IPlugin
    {
        string Topic { get; }

        string PluginName { get; }

        bool IsAsync { get; }

        bool Action(string text);

        Task<bool> ActionAsync(string text);

        bool QuitAction();
    }
}
