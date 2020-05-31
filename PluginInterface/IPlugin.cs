using System.Threading.Tasks;

namespace PluginInterface
{
    public interface IPlugin
    {
        string Topic { get; }

        string PluginName { get; }

        Task<Result> ActionAsync(string text);

        bool QuitAction();
    }

    public class Result
    {
        public ResultStatus Status { get; set; } = ResultStatus.SuccessOnApp;

        public string Message { get; set; } = "";

        public string StackTrace { get; set; } = "";
    }

    public enum ResultStatus
    {
        Undefined = 0,
        SuccessOnApp = 1,
        SuccessOnAppHasMessage = 2,
        FailedOnApp = 3,
        ErrorOnSystem = 4
    }
}
