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
    public class BrokerSession : IAriClient
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

        protected void OnAppDequeue(string message, IConsumer sender, ulong deliveryTag)
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

        protected void FireEvent(string eventName, object eventArgs, IAriClient sender)
        {
            switch (eventName)
            {
                case "ChannelCallerId":
                    if (OnChannelCallerIdEvent != null)
                        OnChannelCallerIdEvent(sender, (ChannelCallerIdEvent) eventArgs);
                    break;


                case "ChannelDtmfReceived":
                    if (OnChannelDtmfReceivedEvent != null)
                        OnChannelDtmfReceivedEvent(sender, (ChannelDtmfReceivedEvent) eventArgs);
                    break;


                case "BridgeCreated":
                    if (OnBridgeCreatedEvent != null)
                        OnBridgeCreatedEvent(sender, (BridgeCreatedEvent) eventArgs);
                    break;


                case "ChannelCreated":
                    if (OnChannelCreatedEvent != null)
                        OnChannelCreatedEvent(sender, (ChannelCreatedEvent) eventArgs);
                    break;


                case "ApplicationReplaced":
                    if (OnApplicationReplacedEvent != null)
                        OnApplicationReplacedEvent(sender, (ApplicationReplacedEvent) eventArgs);
                    break;


                case "ChannelStateChange":
                    if (OnChannelStateChangeEvent != null)
                        OnChannelStateChangeEvent(sender, (ChannelStateChangeEvent) eventArgs);
                    break;


                case "PlaybackFinished":
                    if (OnPlaybackFinishedEvent != null)
                        OnPlaybackFinishedEvent(sender, (PlaybackFinishedEvent) eventArgs);
                    break;


                case "RecordingStarted":
                    if (OnRecordingStartedEvent != null)
                        OnRecordingStartedEvent(sender, (RecordingStartedEvent) eventArgs);
                    break;


                case "ChannelLeftBridge":
                    if (OnChannelLeftBridgeEvent != null)
                        OnChannelLeftBridgeEvent(sender, (ChannelLeftBridgeEvent) eventArgs);
                    break;


                case "ChannelDestroyed":
                    if (OnChannelDestroyedEvent != null)
                        OnChannelDestroyedEvent(sender, (ChannelDestroyedEvent) eventArgs);
                    break;


                case "DeviceStateChanged":
                    if (OnDeviceStateChangedEvent != null)
                        OnDeviceStateChangedEvent(sender, (DeviceStateChangedEvent) eventArgs);
                    break;


                case "ChannelTalkingFinished":
                    if (OnChannelTalkingFinishedEvent != null)
                        OnChannelTalkingFinishedEvent(sender, (ChannelTalkingFinishedEvent) eventArgs);
                    break;


                case "PlaybackStarted":
                    if (OnPlaybackStartedEvent != null)
                        OnPlaybackStartedEvent(sender, (PlaybackStartedEvent) eventArgs);
                    break;


                case "ChannelTalkingStarted":
                    if (OnChannelTalkingStartedEvent != null)
                        OnChannelTalkingStartedEvent(sender, (ChannelTalkingStartedEvent) eventArgs);
                    break;


                case "RecordingFailed":
                    if (OnRecordingFailedEvent != null)
                        OnRecordingFailedEvent(sender, (RecordingFailedEvent) eventArgs);
                    break;


                case "BridgeMerged":
                    if (OnBridgeMergedEvent != null)
                        OnBridgeMergedEvent(sender, (BridgeMergedEvent) eventArgs);
                    break;


                case "RecordingFinished":
                    if (OnRecordingFinishedEvent != null)
                        OnRecordingFinishedEvent(sender, (RecordingFinishedEvent) eventArgs);
                    break;


                case "BridgeAttendedTransfer":
                    if (OnBridgeAttendedTransferEvent != null)
                        OnBridgeAttendedTransferEvent(sender, (BridgeAttendedTransferEvent) eventArgs);
                    break;


                case "ChannelEnteredBridge":
                    if (OnChannelEnteredBridgeEvent != null)
                        OnChannelEnteredBridgeEvent(sender, (ChannelEnteredBridgeEvent) eventArgs);
                    break;


                case "BridgeDestroyed":
                    if (OnBridgeDestroyedEvent != null)
                        OnBridgeDestroyedEvent(sender, (BridgeDestroyedEvent) eventArgs);
                    break;


                case "BridgeBlindTransfer":
                    if (OnBridgeBlindTransferEvent != null)
                        OnBridgeBlindTransferEvent(sender, (BridgeBlindTransferEvent) eventArgs);
                    break;


                case "ChannelUserevent":
                    if (OnChannelUsereventEvent != null)
                        OnChannelUsereventEvent(sender, (ChannelUsereventEvent) eventArgs);
                    break;


                case "ChannelDialplan":
                    if (OnChannelDialplanEvent != null)
                        OnChannelDialplanEvent(sender, (ChannelDialplanEvent) eventArgs);
                    break;


                case "ChannelHangupRequest":
                    if (OnChannelHangupRequestEvent != null)
                        OnChannelHangupRequestEvent(sender, (ChannelHangupRequestEvent) eventArgs);
                    break;


                case "ChannelVarset":
                    if (OnChannelVarsetEvent != null)
                        OnChannelVarsetEvent(sender, (ChannelVarsetEvent) eventArgs);
                    break;


                case "EndpointStateChange":
                    if (OnEndpointStateChangeEvent != null)
                        OnEndpointStateChangeEvent(sender, (EndpointStateChangeEvent) eventArgs);
                    break;


                case "Dial":
                    if (OnDialEvent != null)
                        OnDialEvent(sender, (DialEvent) eventArgs);
                    break;


                case "StasisEnd":
                    if (OnStasisEndEvent != null)
                        OnStasisEndEvent(sender, (StasisEndEvent) eventArgs);
                    break;


                case "StasisStart":
                    if (OnStasisStartEvent != null)
                        OnStasisStartEvent(sender, (StasisStartEvent) eventArgs);
                    break;
                default:
                    if (OnUnhandledEvent != null)
                        OnUnhandledEvent(this, (Event) eventArgs);
                    break;
            }
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

        private delegate void AriEventHandler(IAriClient sender, Event e);

        #region Public Properties

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
        public IPlaybacksActions Playbacks { get; set; }
        public IRecordingsActions Recordings { get; set; }
        public ISoundsActions Sounds { get; set; }

        #endregion

        #region Public Events

        public event ChannelCallerIdEventHandler OnChannelCallerIdEvent;
        public event ChannelDtmfReceivedEventHandler OnChannelDtmfReceivedEvent;
        public event BridgeCreatedEventHandler OnBridgeCreatedEvent;
        public event ChannelCreatedEventHandler OnChannelCreatedEvent;
        public event ApplicationReplacedEventHandler OnApplicationReplacedEvent;
        public event ChannelStateChangeEventHandler OnChannelStateChangeEvent;
        public event PlaybackFinishedEventHandler OnPlaybackFinishedEvent;
        public event RecordingStartedEventHandler OnRecordingStartedEvent;
        public event ChannelLeftBridgeEventHandler OnChannelLeftBridgeEvent;
        public event ChannelDestroyedEventHandler OnChannelDestroyedEvent;
        public event DeviceStateChangedEventHandler OnDeviceStateChangedEvent;
        public event ChannelTalkingFinishedEventHandler OnChannelTalkingFinishedEvent;
        public event PlaybackStartedEventHandler OnPlaybackStartedEvent;
        public event ChannelTalkingStartedEventHandler OnChannelTalkingStartedEvent;
        public event RecordingFailedEventHandler OnRecordingFailedEvent;
        public event BridgeMergedEventHandler OnBridgeMergedEvent;
        public event RecordingFinishedEventHandler OnRecordingFinishedEvent;
        public event BridgeAttendedTransferEventHandler OnBridgeAttendedTransferEvent;
        public event ChannelEnteredBridgeEventHandler OnChannelEnteredBridgeEvent;
        public event BridgeDestroyedEventHandler OnBridgeDestroyedEvent;
        public event BridgeBlindTransferEventHandler OnBridgeBlindTransferEvent;
        public event ChannelUsereventEventHandler OnChannelUsereventEvent;
        public event ChannelDialplanEventHandler OnChannelDialplanEvent;
        public event ChannelHangupRequestEventHandler OnChannelHangupRequestEvent;
        public event ChannelVarsetEventHandler OnChannelVarsetEvent;
        public event EndpointStateChangeEventHandler OnEndpointStateChangeEvent;
        public event DialEventHandler OnDialEvent;
        public event StasisEndEventHandler OnStasisEndEvent;
        public event StasisStartEventHandler OnStasisStartEvent;
        public event UnhandledEventHandler OnUnhandledEvent;

        #endregion
    }
}