using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MqttHomeClient.Domain;
using MqttHomeClient.Entities;
using MqttHomeClient.Service;

namespace MqttHomeClient
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    var env = hostingContext.HostingEnvironment;
                    config.SetBasePath(Directory.GetCurrentDirectory());
                    config.AddCommandLine(args);
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
                })
                .ConfigureLogging(logging =>
                {
                    logging.SetMinimumLevel(LogLevel.Debug);
                    logging.AddConsole();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<MqttConfig>(hostContext.Configuration.GetSection("Mqtt"));
                    services.AddHostedService<MqttService>();
                    services.AddSingleton<WakeOnLanLogic>();
                })
                .Build();

            await host.RunAsync();
        }
    }
}
