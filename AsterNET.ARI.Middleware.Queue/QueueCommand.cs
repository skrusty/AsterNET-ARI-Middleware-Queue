using System;
using System.Collections.Generic;
using System.Dynamic;
using Newtonsoft.Json;

namespace AsterNET.ARI.Middleware.Queue
{
    /// <summary>
    ///     Implements the IRestCommand interface in the AsterNET.ARI middleware
    ///     This allows the queue middleware to accept rest commands from
    ///     the middleware and pass them onto the queue
    /// </summary>
    public class QueueCommand : IRestCommand
    {
        public string Path;
        public ExpandoObject QueryString;

        public QueueCommand()
        {
            QueryString = new ExpandoObject();
        }

        public string UniqueId { get; set; }

        public string Url
        {
            get { return Path; }
            set { }
        }

        public string Method { get; set; }

        public string Body
        {
            get
            {
                var rtn = string.Empty;

                rtn += JsonConvert.SerializeObject(QueryString);

                return rtn;
            }
            private set { }
        }

        public void AddUrlSegment(string segName, string value)
        {
            Path = Path.Replace("{" + segName + "}", value);
        }

        public void AddParameter(string name, object value, ParameterType type)
        {
            switch (type)
            {
                case ParameterType.Cookie:
                    break;
                case ParameterType.GetOrPost:
                    break;
                case ParameterType.UrlSegment:
                    AddUrlSegment(name, value.ToString());
                    break;
                case ParameterType.HttpHeader:
                    break;
                case ParameterType.RequestBody:
                    throw new NotImplementedException();
                    break;
                case ParameterType.QueryString:
                    AddQueryString(name, value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
        }

        private void AddQueryString(string name, object value)
        {
            var qP = QueryString as IDictionary<string, object>;
            qP[name] = value;
        }
    }
}