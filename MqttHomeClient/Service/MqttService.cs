using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MqttHomeClient.Domain;
using MqttHomeClient.Entities;
using MqttHomeClient.Entities.Json;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using PluginInterface;
using ZLogger;

namespace MqttHomeClient.Service
{
    internal class MqttService : IHostedService
    {

        private readonly IMqttClient _mqttClient;
        private readonly MqttConfig _mqttConfig;
        private readonly LoadPlugin _loadPlugin;

        private readonly IHostApplicationLifetime _appLifetime;
        private readonly ILogger<MqttService> _logger;

        private readonly List<IPlugin> _plugins;

        public MqttService(
            IHostApplicationLifetime appLifetime,
            LoadPlugin loadPlugin,
            IOptions<MqttConfig> mqttConfig,
            ILogger<MqttService> logger)
        {
            var mqttFactory = new MqttFactory();
            _mqttClient = mqttFactory.CreateMqttClient();
            _mqttConfig = mqttConfig.Value;

            _loadPlugin = loadPlugin;

            _logger = logger;

            _appLifetime = appLifetime;

            _plugins = _loadPlugin.LoadPlugins();

        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _appLifetime.ApplicationStarted.Register(OnStarted);
            _appLifetime.ApplicationStarted.Register(OnStopped);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private async void OnStarted()
        {


            //受信時の処理
            _mqttClient.UseApplicationMessageReceivedHandler(async eventArgs =>
            {
                try
                {
                    var topic = eventArgs.ApplicationMessage.Topic;
                    var payload = Encoding.UTF8.GetString(eventArgs.ApplicationMessage.Payload, 0, eventArgs.ApplicationMessage.Payload.Length);

                    foreach (var plugin in _plugins)
                    {
                        if (topic.Equals($"{_mqttConfig.Channel}/{plugin.Topic}", StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.ZLogInformation($"Topic:{topic} Start");

                            var json = JsonSerializer.Deserialize<MqttResponse>(payload);

                            await PluginProc(plugin, json.Data);


                            _logger.ZLogInformation($"Topic:{topic} End");
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message);
                }
            });

            _mqttClient.UseConnectedHandler(async eventArgs =>
            {
                //指定チャンネルの全Topicを購読
                _ = await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder()
                     .WithTopic($"{_mqttConfig.Channel}/#")
                     .Build());
            });

            _mqttClient.UseDisconnectedHandler(async eventArgs =>
            {
                _logger.LogWarning("MQTTBrokerから切断されました。再接続します。");

                await Connect();
            });

            await Connect();
        }

        private async void OnStopped()
        {
            foreach (var plugin in _plugins)
            {
                plugin.QuitAction();
            }
            await _mqttClient.DisconnectAsync();
        }

        /// <summary>
        /// MQTT Brokerへ接続
        /// </summary>
        /// <returns></returns>
        public async Task Connect()
        {
            if (string.IsNullOrEmpty(_mqttConfig.BrokerHostname))
            {
                _logger.ZLogWarning("MQTTBrokerホスト名が未入力です。");
                throw new ArgumentNullException(nameof(MqttConfig.BrokerHostname));
            }

            if (_mqttConfig.BrokerHostPort == 0)
            {
                _logger.ZLogWarning("MQTTBrokerポート番号が適切ではありません。");
                throw new ArgumentNullException(nameof(MqttConfig.BrokerHostPort));
            }

            if (string.IsNullOrEmpty(_mqttConfig.AccountId))
            {
                _logger.ZLogWarning("MQTTBrokerアカウントIDが未入力です。");
                throw new ArgumentNullException(nameof(MqttConfig.AccountId));
            }

            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(_mqttConfig.BrokerHostname, _mqttConfig.BrokerHostPort)
                .WithCredentials(_mqttConfig.AccountId, _mqttConfig.AccountPassword)
                .WithTls()
                .WithCleanSession()
                .Build();

            var retry = 0;

            while (!_mqttClient.IsConnected && retry < 10)
            {
                try
                {
                    await _mqttClient.ConnectAsync(mqttClientOptions);
                    _logger.ZLogInformation("MQTT Connected.");
                }
                catch (Exception e)
                {
                    _logger.ZLogWarning($"接続失敗 {retry + 1}回目:{e.Message}");
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
                retry++;
            }
        }

        public async Task PluginProc(IPlugin plugin, string msg)
        {
            var result = await plugin.ActionAsync(msg);

            switch (result.Status)
            {
                case ResultStatus.SuccessOnApp:
                    break;
                case ResultStatus.SuccessOnAppHasMessage:
                    _logger.ZLogInformation(result.Message);
                    break;
                case ResultStatus.FailedOnApp:
                    _logger.ZLogWarning(result.Message);
                    break;
                case ResultStatus.ErrorOnSystem:
                    _logger.ZLogError(result.Message);
                    _logger.ZLogError(result.StackTrace);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}