using System;

namespace AsterNET.ARI.Middleware.Queue.QueueProviders
{
	public enum MessageFinalResponse
	{
		Accept,
		Reject,
		RejectWithReQueue
	}
    public interface IQueueProvider
    {
        /// <summary>
        /// Create the consumer which will read in new dialogue messages from the provider
        /// </summary>
        /// <param name="applicationName"></param>
        /// <returns></returns>
        IConsumer CreateAppConsumer(string applicationName);

        /// <summary>
        /// Create a new consumer that will read in new events from a dialogue
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="dialogId"></param>
        /// <returns></returns>
        IConsumer CreateConsumer(string queueName, string dialogId);

        /// <summary>
        /// Create a new producer that will push new messages to the provider for a dialogue
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="dialogId"></param>
        /// <returns></returns>
        IProducer CreateProducer(string queueName, string dialogId);
    }

    public interface IProducer
    {
        string QueueName { get; set; }
        string DialogId { get; set; }
        void PushToQueue(string message);
        void Close();
	    void Teminate();
    }

    public interface IConsumer
    {
        /// <summary>
        ///     Gets or sets the name of the queue.
        /// </summary>
        /// <value>The name of the queue.</value>
        string QueueName { get; set; }

        string DialogId { get; set; }

        /// <summary>
        ///     Read a message from the queue.
        /// </summary>
        /// <param name="onDequeue">The action to take when receiving a message</param>
        /// <param name="onError">If an error occurs, provide an action to take.</param>
        /// <param name="exchangeName">Name of the exchange.</param>
        /// <param name="queueName">Name of the queue.</param>
        /// <param name="routingKeyName">Name of the routing key.</param>
        void ReadFromQueue(Func<string, IConsumer, ulong, MessageFinalResponse> onDequeue, Action<Exception, IConsumer, ulong> onError);

        void StopReading();
        void Close();
	    void Terminate();
    }
}