using Newtonsoft.Json;

namespace AsterNET.ARI.Middleware.Queue.Messages
{
    /// <summary>
    ///     This is the message sent by the proxy to inform our client of a new
    ///     channel entering our application. The Dialog ID will be used to
    ///     get the subscription topic for this conversation
    /// </summary>
    public class NewDialogInfo
    {
        public string Application { get; set; }

        [JsonProperty("dialog_id")]
        public string DialogId { get; set; }

        [JsonProperty("server_id")]
        public string ServerId { get; set; }
    }
}