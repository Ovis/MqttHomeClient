using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MqttHomeClient.Entities;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;

namespace MqttHomeClient.Service
{
    internal class MqttService : IHostedService
    {
        private readonly IMqttClient _mqttClient;
        private readonly MqttConfig _mqttConfig;

        private readonly IHostApplicationLifetime _appLifetime;
        private readonly ILogger<MqttService> _logger;

        public MqttService(IHostApplicationLifetime appLifetime, IOptions<MqttConfig> mqttConfig, ILogger<MqttService> logger)
        {
            var mqttFactory = new MqttFactory();
            _mqttClient = mqttFactory.CreateMqttClient();
            _mqttConfig = mqttConfig.Value;

            _logger = logger;

            _appLifetime = appLifetime;

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
            _mqttClient.UseApplicationMessageReceivedHandler(eventArgs =>
            {
                try
                {
                    var topic = eventArgs.ApplicationMessage.Topic;
                    var payload = Encoding.UTF8.GetString(eventArgs.ApplicationMessage.Payload, 0, eventArgs.ApplicationMessage.Payload.Length);

                    _logger.LogInformation($"topic:{topic}");
                    _logger.LogInformation($"payload:{payload}");

                    if (topic.Equals($"{_mqttConfig.Channel}/topic1", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation($"topic:{topic}/topic1");
                        _logger.LogInformation($"payload:{payload}");
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message);
                    _appLifetime.StopApplication();
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