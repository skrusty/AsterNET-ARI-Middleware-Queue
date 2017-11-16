using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AsterNET.ARI.Proxy.Common.Messages;
using AsterNET.ARI.Middleware.Queue.QueueProviders;
using Newtonsoft.Json;

namespace AsterNET.ARI.Middleware.Queue
{
    public class ActionConsumer : IActionConsumer, IDisposable
    {
        private readonly IProducer _actionRequestConsumer;
        private readonly IConsumer _actionResponseProducer;
        private readonly Dictionary<string, TaskCompletionSource<CommandResult>> _openRequests;
        private readonly int _actionTimeout;

        public ActionConsumer(IProducer actionRequestConsumer, IConsumer actionResponseProducer)
        {
            _actionRequestConsumer = actionRequestConsumer;
            _actionResponseProducer = actionResponseProducer;
            _actionTimeout = 1000;
            _openRequests = new Dictionary<string, TaskCompletionSource<CommandResult>>();
        }

        public ActionConsumer(IProducer actionRequestConsumer, IConsumer actionResponseProducer, int actionTimeout)
        {
            _actionRequestConsumer = actionRequestConsumer;
            _actionResponseProducer = actionResponseProducer;
            _actionTimeout = actionTimeout;

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
            try
            {
                var proxyCommand = new Command
                {
                    Method = command.Method,
                    UniqueId = command.UniqueId,
                    Url = command.Url,
                    Body = command.Body
                };

                var tcs = new TaskCompletionSource<CommandResult>();
                var ct = new CancellationTokenSource(_actionTimeout);
                ct.Token.Register(() =>
                {
                    tcs.TrySetCanceled();
                }, useSynchronizationContext: false);

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
            catch (TaskCanceledException tEx)
            {
                // Task was cancelled!
#if DEBUG
                Debug.WriteLine("Setting cancellation token for action {0} on dialogue {1}", command.Url, _actionRequestConsumer.DialogId);
#endif
                return null;
            }
            catch (DialogueClosedException dEx)
            {
#if DEBUG
                Debug.WriteLine("Tried to execute action {0} on dialogue {1} but Dialogue was closed.", command.Url, _actionRequestConsumer.DialogId);
#endif
                return null;
            }

        }

        public IRestCommandResult ProcessRestCommand(IRestCommand command)
        {
            try
            {
                var proxyCommand = new Command
                {
                    Method = command.Method,
                    UniqueId = command.UniqueId,
                    Url = command.Url,
                    Body = command.Body
                };

                var tcs = new TaskCompletionSource<CommandResult>();
                var ct = new CancellationTokenSource(_actionTimeout);
                ct.Token.Register(() =>
                {
                    tcs.TrySetCanceled();
                }, useSynchronizationContext: false);

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
            catch (AggregateException e)
            {
                foreach (var ex in e.InnerExceptions)
                {
                    if (ex is TaskCanceledException)
                    {
                        // Task was cancelled!
#if DEBUG
                        Debug.WriteLine("Setting cancellation token for action {0} on dialogue {1}", command.Url, _actionRequestConsumer.DialogId);
#endif       
                    }
                }
                return null;
            }
            catch (DialogueClosedException dEx)
            {
                throw new DialogueClosedException()
                {
                    DialogueId = _actionRequestConsumer.DialogId
                };
            }
        }

        public async Task<IRestCommandResult<T>> ProcessRestTaskCommand<T>(IRestCommand command) where T : new()
        {
            return await Task.Run(async () =>
            {
                return ProcessRestCommand<T>(command);
            });
        }

        public async Task<IRestCommandResult> ProcessRestTaskCommand(IRestCommand command)
        {
            return await Task.Run(async () =>
            {
                return ProcessRestCommand(command);
            });
        }

        protected MessageFinalResponse OnAppDequeue(string message, IConsumer sender, ulong deliveryTag)
        {
#if DEBUG
            Debug.WriteLine(message);
#endif
            var restResponse = (CommandResult) JsonConvert.DeserializeObject(message, typeof (CommandResult));
			if (!_openRequests.ContainsKey(restResponse.UniqueId))
				return MessageFinalResponse.Reject;

            // Complete the request flow back to the originating Process command
            _openRequests[restResponse.UniqueId].TrySetResult(restResponse);

	        return MessageFinalResponse.Accept;
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