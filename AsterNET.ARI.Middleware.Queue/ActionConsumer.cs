using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using AsterNET.ARI.Middleware.Queue.Messages;
using AsterNET.ARI.Middleware.Queue.QueueProviders;
using Newtonsoft.Json;

namespace AsterNET.ARI.Middleware.Queue
{
    public class ActionConsumer : IActionConsumer, IDisposable
    {
        private readonly IProducer _actionRequestConsumer;
        private readonly IConsumer _actionResponseProducer;
        private readonly Dictionary<string, TaskCompletionSource<CommandResult>> _openRequests;

        public ActionConsumer(IProducer actionRequestConsumer, IConsumer actionResponseProducer)
        {
            _actionRequestConsumer = actionRequestConsumer;
            _actionResponseProducer = actionResponseProducer;

            _openRequests = new Dictionary<string, TaskCompletionSource<CommandResult>>();
        }

        public void Start()
        {
            _actionResponseProducer.ReadFromQueue(OnAppDequeue, OnError);
        }

        public IRestCommand GetRestCommand(HttpMethod method, string path)
        {
            return new QueueCommand
            {
                UniqueId = Guid.NewGuid().ToString().Replace("-", ""),
                Method = method.ToString(),
                Path = path
            };
        }

        public IRestCommandResult<T> ProcessRestCommand<T>(IRestCommand command) where T : new()
        {
            var proxyCommand = new Command
            {
                Method = command.Method,
                UniqueId = command.UniqueId,
                Url = command.Url,
                Body = command.Body
            };

            var tcs = new TaskCompletionSource<CommandResult>();
            _openRequests.Add(command.UniqueId, tcs);
            var request = JsonConvert.SerializeObject(proxyCommand);
#if DEBUG
            Debug.WriteLine(request);
#endif
            _actionRequestConsumer.PushToQueue(request);

            // await for the result
            var result = tcs.Task.Result;
            var rtn = new QueueCommandResult<T>
            {
                UniqueId = result.UniqueId,
                StatusCode = (HttpStatusCode) result.StatusCode
            };
            if (!string.IsNullOrEmpty(result.ResponseBody))
                rtn.Data = JsonConvert.DeserializeObject<T>(result.ResponseBody);

            return rtn;
        }

        public IRestCommandResult ProcessRestCommand(IRestCommand command)
        {
            var proxyCommand = new Command
            {
                Method = command.Method,
                UniqueId = command.UniqueId,
                Url = command.Url,
                Body = command.Body
            };

            var tcs = new TaskCompletionSource<CommandResult>();
            _openRequests.Add(command.UniqueId, tcs);
            var request = JsonConvert.SerializeObject(proxyCommand);
#if DEBUG
            Debug.WriteLine(request);
#endif
            _actionRequestConsumer.PushToQueue(request);

            // await for the result
            var result = tcs.Task.Result;
            var rtn = new QueueCommandResult
            {
                UniqueId = result.UniqueId,
                StatusCode = (HttpStatusCode) result.StatusCode
            };

            return rtn;
        }

        protected void OnAppDequeue(string message, IConsumer sender, ulong deliveryTag)
        {
#if DEBUG
            Debug.WriteLine(message);
#endif
            var restResponse = (CommandResult) JsonConvert.DeserializeObject(message, typeof (CommandResult));
            if (!_openRequests.ContainsKey(restResponse.UniqueId))
                return;

            // Complete the request flow back to the originating Process command
            _openRequests[restResponse.UniqueId].SetResult(restResponse);
        }

        protected void OnError(Exception ex, IConsumer sender, ulong deliveryTag)
        {
        }

        public void Dispose()
        {
            // Close queue interfaces
            _actionRequestConsumer.Close();
            _actionResponseProducer.Close();
        }
    }
}