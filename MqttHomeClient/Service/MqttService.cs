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

namespace MqttHomeClient.Service
{
    internal class MqttService : IHostedService
    {
        private readonly IMqttClient _mqttClient;
        private readonly MqttConfig _mqttConfig;

        private readonly IHostApplicationLifetime _appLifetime;
        private readonly ILogger<MqttService> _logger;

        private readonly List<IPlugin> _plugins;

        public MqttService(
            IHostApplicationLifetime appLifetime,
            IOptions<MqttConfig> mqttConfig,
            ILogger<MqttService> logger)
        {
            var mqttFactory = new MqttFactory();
            _mqttClient = mqttFactory.CreateMqttClient();
            _mqttConfig = mqttConfig.Value;

            _logger = logger;

            _appLifetime = appLifetime;

            _plugins = LoadPlugin.LoadPlugins();

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
                            var json = JsonSerializer.Deserialize<MqttResponse>(payload);

                            var result = await plugin.ActionAsync(json.Data);

                            if (result == false)
                            {
                                _logger.LogWarning($"トピック{topic}の処理中、{plugin.PluginName}プラグインの内部で異常が発生しました。");
                            }
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
                _ = await _mqttClient.SubscribeAsync(new TopicFilterBuilder()
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
                throw new ArgumentNullException(nameof(MqttConfig.BrokerHostname));
            }

            if (_mqttConfig.BrokerHostPort == 0)
            {
                throw new ArgumentNullException(nameof(MqttConfig.BrokerHostPort));
            }

            if (string.IsNullOrEmpty(_mqttConfig.AccountId))
            {
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
                    _logger.LogInformation("Connected.");
                }
                catch (Exception e)
                {
                    _logger.LogWarning($"接続失敗 {retry + 1}回目:{e.Message}");
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
                retry++;
            }
        }
    }
}