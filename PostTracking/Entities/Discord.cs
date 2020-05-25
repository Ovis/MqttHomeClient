using System;
using System.Text.Json.Serialization;

namespace PostTracking.Entities
{
    [Serializable]
    sealed class Discord
    {
        [JsonPropertyName("content")]
        public string Content { get; set; }
    }
}