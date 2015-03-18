using Newtonsoft.Json;

namespace AsterNET.ARI.Middleware.Queue.Messages
{
    /// <summary>
    /// The result of a Command passed to the bus and returned with
    /// the same unique_id
    /// </summary>
    public class CommandResult
    {
        [JsonProperty("unique_id")]
        public string UniqueId { get; set; }

        [JsonProperty("status_code")]
        public int StatusCode { get; set; }

        [JsonProperty("response_body")]
        public string ResponseBody { get; set; }
    }
}