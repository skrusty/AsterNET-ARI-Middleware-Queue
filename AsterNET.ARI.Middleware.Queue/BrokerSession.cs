using System;
using System.Diagnostics;
using System.Runtime.Remoting.Messaging;
using AsterNET.ARI.Actions;
using AsterNET.ARI.Middleware.Queue.QueueProviders;
using AsterNET.ARI.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AsterNET.ARI.Middleware.Queue
{
    /// <summary>
    ///     Acts as a session manager for the current Dialogue between AriClient and
    ///     the Ari Proxy service
    /// </summary>
    public class BrokerSession : BaseAriClient, IAriClient
    {
        private readonly AriBrokerClient _client;
        private readonly ActionConsumer _dialogueActionConsumer;

        public BrokerSession(AriBrokerClient client,
            string dialogueId,
            string serverId,
            IConsumer eventQueue,
            IConsumer actionResponseQueue,
            IProducer actionRequestQueue)
        {
            _client = client;
            DialogueId = dialogueId;
            ServerId = serverId;
            EventQueue = eventQueue;
            ActionResponseQueue = actionResponseQueue;
            ActionRequestQueue = actionRequestQueue;

            InternalEvent += ARIClient_internalEvent;

            // Create Action Consumer for Session
            _dialogueActionConsumer = new ActionConsumer(actionRequestQueue, actionResponseQueue);

            // Init Actions
            Asterisk = new AsteriskActions(_dialogueActionConsumer);
            Applications = new ApplicationsActions(_dialogueActionConsumer);
            Bridges = new BridgesActions(_dialogueActionConsumer);
            Channels = new ChannelsActions(_dialogueActionConsumer);
            DeviceStates = new DeviceStatesActions(_dialogueActionConsumer);
            Endpoints = new EndpointsActions(_dialogueActionConsumer);
            Events = new EventsActions(_dialogueActionConsumer);
            Playbacks = new PlaybacksActions(_dialogueActionConsumer);
            Recordings = new RecordingsActions(_dialogueActionConsumer);
            Sounds = new SoundsActions(_dialogueActionConsumer);
        }

        private event AriEventHandler InternalEvent;

        protected MessageFinalResponse OnAppDequeue(string message, IConsumer sender, ulong deliveryTag)
        {
#if DEBUG
            Debug.WriteLine(message);
#endif
            // load the message
            try
            {
                var jsonMsg = (JObject) JToken.Parse(message);
                var eventName = jsonMsg.SelectToken("type").Value<string>();

                var type = _client.Asm.GetType("AsterNET.ARI.Models." + eventName + "Event");

                // Get the instance of BrokeredSession for this dialogue and pass as sender
                // to InternalEvent

                if (type != null)
                    InternalEvent.BeginInvoke(this,
                        (Event) JsonConvert.DeserializeObject(jsonMsg.SelectToken("ari_body").Value<string>(), type),
                        EventComplete,
                        null);
                else
                    InternalEvent.BeginInvoke(this,
                        (Event)
                            JsonConvert.DeserializeObject(jsonMsg.SelectToken("ari_body").Value<string>(),
                                typeof (Event)),
                        EventComplete, null);
            }
            catch (Exception ex)
            {
#if DEBUG
                Debug.WriteLine("Raise Event Failed: ", ex.Message);
#endif
            }

	        return MessageFinalResponse.Accept;
        }

        protected void OnError(Exception ex, IConsumer sender, ulong deliveryTag)
        {
#if DEBUG
            Debug.WriteLine("IQueueProvider Error: ", ex.Message);
#endif
        }

        private static void EventComplete(IAsyncResult result)
        {
            var ar = (AsyncResult) result;
            var invokedMethod = (AriEventHandler) ar.AsyncDelegate;

            try
            {
                invokedMethod.EndInvoke(result);
            }
            catch (Exception ex)
            {
                // Handle any exceptions that were thrown by the invoked method
#if DEBUG
                Debug.WriteLine("An Event Listener Threw Unhandled Exception: ", ex.Message);
#endif
            }
        }

        private void ARIClient_internalEvent(IAriClient sender, Event e)
        {
            FireEvent(e.Type, e, sender);
        }

        public void Start()
        {
            // Start the event queue listener
            EventQueue.ReadFromQueue(OnAppDequeue, OnError);

            // Start the action consumer
            _dialogueActionConsumer.Start();
        }

        public void Stop()
        {
            EventQueue.StopReading();
            EventQueue.Close();
        }

	    public void CloseDialogue()
	    {
		    // delete all queues
		    EventQueue.Terminate();
		    ActionRequestQueue.Teminate();
		    ActionResponseQueue.Terminate();
			// remove active dialogue
		    _client.ActiveDialogs.Remove(DialogueId);
	    }

        private delegate void AriEventHandler(IAriClient sender, Event e);

        #region Public Properties

	    public AriBrokerClient Client
	    {
		    get { return _client; }
	    }

	    public string DialogueId { get; set; }
        public string ServerId { get; set; }
        public IConsumer EventQueue { get; set; }
        public IConsumer ActionResponseQueue { get; set; }
        public IProducer ActionRequestQueue { get; set; }
        public IAsteriskActions Asterisk { get; set; }
        public IApplicationsActions Applications { get; set; }
        public IBridgesActions Bridges { get; set; }
        public IChannelsActions Channels { get; set; }
        public IDeviceStatesActions DeviceStates { get; set; }
        public IEndpointsActions Endpoints { get; set; }
        public IEventsActions Events { get; set; }
        public IMailboxesActions Mailboxes { get; set; }
        public IPlaybacksActions Playbacks { get; set; }
        public IRecordingsActions Recordings { get; set; }
        public ISoundsActions Sounds { get; set; }

        #endregion

    }
}