using System;
using System.Diagnostics;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace AsterNET.ARI.Middleware.Queue.QueueProviders
{
    /// <summary>
    /// 
    /// </summary>
    public class RabbitMq : IQueueProvider
    {
        private readonly RabbitMqOptions _options;
        private readonly ConnectionFactory _rmqConnection;

        public RabbitMq(string amqp)
        {
            _rmqConnection = new ConnectionFactory {uri = new Uri(amqp)};
            _options = new RabbitMqOptions()
            {
                AutoDelete = false,
                Durable = true,
                Exclusive = false
            };
        }

        public RabbitMq(string amqp, RabbitMqOptions options)
        {
            _rmqConnection = new ConnectionFactory { uri = new Uri(amqp) };
            _options = options;
        }

        public IConsumer CreateConsumer(string queueName, string dialogId)
        {
            return new RabbitMqConsumer(_rmqConnection.CreateConnection(), queueName, _options)
            {
                DialogId = dialogId
            };
        }

        public IProducer CreateProducer(string queueName, string dialogId)
        {
            return new RabbitMqProducer(_rmqConnection.CreateConnection(), queueName, _options)
            {
                DialogId = dialogId
            };
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class RabbitMqConsumer : IConsumer, IDisposable
    {
        private readonly RabbitMqOptions _options;
        private EventingBasicConsumer _consumer;

        public RabbitMqConsumer(IConnection connection, string queueName, RabbitMqOptions options)
        {
            _options = options;
            Connection = connection;
            QueueName = queueName;

            CreateModel();
        }

        /// <summary>
        ///     Gets or sets the model.
        /// </summary>
        /// <value>The model.</value>
        private IModel Model { get; set; }

        /// <summary>
        ///     Gets or sets the connection to rabbit
        /// </summary>
        /// <value>The connection to rabbit</value>
        public IConnection Connection { get; set; }

        /// <summary>
        ///     Gets or sets the name of the queue.
        /// </summary>
        /// <value>The name of the queue.</value>
        public string QueueName { get; set; }

        public string DialogId { get; set; }

        /// <summary>
        ///     Read a message from the queue.
        /// </summary>
        /// <param name="onDequeue">The action to take when receiving a message</param>
        /// <param name="onError">If an error occurs, provide an action to take.</param>
        public void ReadFromQueue(Func<string, IConsumer, ulong, MessageFinalResponse> onDequeue,
            Action<Exception, IConsumer, ulong> onError)
        {
            _consumer = new EventingBasicConsumer(Model);

            // Receive the message from the queue and act on that message
            _consumer.Received += (o, e) =>
            {
                try
                {
                    var queuedMessage = Encoding.ASCII.GetString(e.Body);
					var accepted = onDequeue.Invoke(queuedMessage, this, e.DeliveryTag);

					switch (accepted)
					{
						case MessageFinalResponse.Accept:
							Model.BasicAck(e.DeliveryTag, false);
							break;
						case MessageFinalResponse.RejectWithReQueue:
							Model.BasicReject(e.DeliveryTag, true);
							break;
						case MessageFinalResponse.Reject:
							Model.BasicReject(e.DeliveryTag, false);
							break;
					}
                }
                catch (Exception ex)
                {
#if DEBUG
                    Debug.WriteLine(ex.Message);
#endif
                }
            };

            Model.BasicConsume(QueueName, false, _consumer);
        }

        public void StopReading()
        {
            Model.BasicCancel(_consumer.ConsumerTag);
        }

        public void Close()
        {
            Connection.Close();
        }

	    public void Terminate()
	    {
		    StopReading();
		    Model.QueueDelete(QueueName, false, false);
		    Close();
	    }

	    public void Dispose()
        {
            Connection.Close();
        }

        private void CreateModel()
        {
            Model = Connection.CreateModel();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class RabbitMqProducer : IProducer, IDisposable
    {
        public RabbitMqProducer(IConnection connection, string queueName, RabbitMqOptions options)
        {
            Connection = connection;
            QueueName = queueName;

            CreateModel();
            Model.QueueDeclare(QueueName, options.Durable, options.Exclusive, options.AutoDelete, null);
        }

        private IModel Model { get; set; }
        public IConnection Connection { get; set; }

        public void Dispose()
        {
            Connection.Close();
        }

        public string QueueName { get; set; }
        public string DialogId { get; set; }

        public void PushToQueue(string message)
        {
            var body = Encoding.UTF8.GetBytes(message);

            Model.BasicPublish("", QueueName, null, body);
        }

        public void Close()
        {
            Connection.Close();
        }

	    public void Teminate()
	    {
			Model.QueueDelete(QueueName, false, false);
		    Close();
	    }

	    private void CreateModel()
        {
            Model = Connection.CreateModel();
        }
    }

    public class RabbitMqOptions
    {
        public bool Durable { get; set; }
        public bool AutoDelete { get; set; }
        public bool Exclusive { get; set; }
    }
}