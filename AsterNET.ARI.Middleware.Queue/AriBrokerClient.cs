using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using AsterNET.ARI.Middleware.Queue.Messages;
using AsterNET.ARI.Middleware.Queue.QueueProviders;
using Newtonsoft.Json;

namespace AsterNET.ARI.Middleware.Queue
{
    /// <summary>
    ///     The AriBrokerClient provides functionality like that of the AriClient
    ///     but allows for message flow that is per application based, allowing
    ///     middleware that talks to queues for example to behave in the correct way.
    /// </summary>
    public class AriBrokerClient
    {
        private readonly Dictionary<string, BrokerSession> _applicationQueues;
        private readonly string _appName;
        private readonly IQueueProvider _queueProvider;
        private IConsumer _applicationPrimaryConsumer;
        protected IEventProducer EventProducer;
        internal Assembly Asm;

        public event EventHandler<BrokerSession> OnNewDialogue;
        public event EventHandler<Guid> OnDialogueClosed;
		/// <summary>
		/// ActiveDialogueLimit restricts the number of active dialogue on the client to rejecting (with requeue)
		/// any new dialogues that are presentde after the limit has been reached. Once there is room for new
		/// dialogues, they will be accepted again.
		/// </summary>
		public int ActiveDialogueLimit { get; set; }

        public AriBrokerClient(string appName, IQueueProvider queueProvider)
        {
            Init();
            _appName = appName;
            _queueProvider = queueProvider;
            _applicationQueues = new Dictionary<string, BrokerSession>();
        }

        private void Init()
        {
            // Setup assembly search for event models
            // NOTE: this should maybe come from a central source, as this could get messy
            Asm = AppDomain.CurrentDomain.GetAssemblies().ToList().Single(x => x.GetName().Name == "AsterNET.ARI");

			// Set default dialogue limit
	        ActiveDialogueLimit = 50;
        }

        public void Connect()
        {
            _applicationPrimaryConsumer = _queueProvider.CreateAppConsumer(_appName);
            _applicationPrimaryConsumer.ReadFromQueue(OnDequeue, OnError);
        }

        /// <summary>
        /// Stops the Broker reading new Dialog events from the message bus.
        /// It does not stop already accepted dialogs from running
        /// </summary>
        public void Stop()
        {
            _applicationPrimaryConsumer.StopReading();
        }

        public Dictionary<string, BrokerSession> ActiveDialogs
        {
            get { return _applicationQueues; }
        }

        protected MessageFinalResponse OnDequeue(string message, IConsumer sender, ulong deliveryTag)
        {
#if DEBUG
            Debug.WriteLine(message);
#endif

			// Check Dialogue Limit
	        if (ActiveDialogs.Count >= ActiveDialogueLimit)
	        {
		        Debug.WriteLine("Rejected new dialogue due to ActiveDialogueLimit ({0}) being exceeded {1}", ActiveDialogueLimit, ActiveDialogs.Count);
		        return MessageFinalResponse.RejectWithReQueue; // Requeue dialogue
	        }

	        // A new instance has been passed to us
            var newInstance =
                (NewDialogInfo) JsonConvert.DeserializeObject(message, typeof (NewDialogInfo));
            
            // Create new message queues
            var newApp = new BrokerSession(
                this,
                newInstance.DialogId,
                newInstance.ServerId,
                _queueProvider.CreateConsumer("events_" + newInstance.DialogId, newInstance.DialogId),
                _queueProvider.CreateConsumer("responses_" + newInstance.DialogId, newInstance.DialogId),
                _queueProvider.CreateProducer("commands_" + newInstance.DialogId, newInstance.DialogId));

            // Record this new instance
            _applicationQueues.Add(newInstance.DialogId, newApp);

            // Raise event NewDialog (event runner has not yet been started!)
            if (OnNewDialogue != null) OnNewDialogue(this, newApp);

            // Start the event runner
            newApp.Start();

	        return MessageFinalResponse.Accept;
        }

        protected void OnError(Exception ex, IConsumer sender, ulong deliveryTag)
        {
            Console.WriteLine(ex.Message);
        }


        internal void DialogueClosed(Guid dialogueId)
        {
            OnDialogueClosed?.Invoke(this, dialogueId);
        }
    }
}