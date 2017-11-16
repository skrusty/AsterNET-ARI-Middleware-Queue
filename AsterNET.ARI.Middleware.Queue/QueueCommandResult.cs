using System.Net;

namespace AsterNET.ARI.Middleware.Queue
{
    /// <summary>
    /// Local implementation of IRestCommandResult
    /// </summary>
    public class QueueCommandResult : IRestCommandResult
    {
        public string UniqueId { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public byte[] RawData { get; set; }
    }

    /// <summary>
    /// Local implementation of IRestCommandResult with a return type T
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class QueueCommandResult<T> : IRestCommandResult<T>
        where T : new()
    {
        public string UniqueId { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public T Data { get; set; }
    }
}