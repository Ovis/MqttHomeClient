namespace MqttHomeClient.Entities
{
    public class MqttConfig
    {
        /// <summary>
        /// MQTT Brokerホスト名
        /// </summary>
        public string BrokerHostname { get; set; }


        /// <summary>
        /// MQTT Brokerポート番号
        /// </summary>
        public int BrokerHostPort { get; set; }


        /// <summary>
        /// チャンネル名
        /// </summary>
        public string Channel { get; set; }


        /// <summary>
        /// MQTT ユーザーID/トークン(Beebotteの場合)
        /// </summary>
        public string AccountId { get; set; }


        /// <summary>
        /// MQTT ユーザーパスワード
        /// </summary>
        public string AccountPassword { get; set; }

    }
}