using System.Text.Json.Serialization;

namespace MqttHomeClient.Entities.Json
{
    public class MqttResponse
    {
        /// <summary>
        /// 受け取る値
        /// </summary>
        [JsonPropertyName("data")]
        public string Data { get; set; }
    }
}
