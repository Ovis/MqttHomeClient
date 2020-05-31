using System;
using Newtonsoft.Json;

namespace PostTracking.Entities
{
    [Serializable]
    sealed class Discord
    {
        [JsonProperty("content")]
        public string Content { get; set; }
    }
}