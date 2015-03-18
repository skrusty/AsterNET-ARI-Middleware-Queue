using Newtonsoft.Json;

namespace AsterNET.ARI.Middleware.Queue.Messages
{
    public class Command
    {
        [JsonProperty("unique_id")]
        public string UniqueId { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("body")]
        public string Body { get; set; }
    }
}