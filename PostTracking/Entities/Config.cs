using System.IO;
using System.Text.Json;

namespace PostTracking.Entities
{
    internal class Config
    {
        private string configFileName = "PostTrackingConfig.json";

        public Config GetConfig()
        {
            var dic = Directory.GetCurrentDirectory();
            var path = Path.Combine(dic, "Plugins", configFileName);

            try
            {
                var jsonString = File.ReadAllText(path);

                return JsonSerializer.Deserialize<Config>(jsonString);
            }
            catch
            {
                return null;
            }
        }
        /// <summary>
        /// WebHookUrl
        /// </summary>
        public string WebHookUrl { get; set; }
    }
}
