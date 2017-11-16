using System;
using AsterNET.ARI;
using AsterNET.ARI.Middleware.Queue;
using AsterNET.ARI.Middleware.Queue.QueueProviders;
using AsterNET.ARI.Models;
using AsterNET.ARI.Middleware.Queue.RabbitMQ;

namespace QueueTest
{
    class Program
    {
        static void Main(string[] args)
        {
            
            // Create Ari client that handles brokered messages (unlike standard AriClient)
            // This will help to ensure correlation is supported in our message flow
            var ari = new AriBrokerClient("test",
                new RabbitMq("amqp://", new RabbitMqOptions()
                {
                    // App Queue Options
                    AutoDelete = false,
                    Durable = true,
                    Exclusive = false
                }, new RabbitMqOptions()
                {
                    // Dialogue Queue Options
                    AutoDelete = false,
                    Durable = true,
                    Exclusive = false
                }));

            // Hook into OnNewDialog event
            ari.OnNewDialogue += AriOnNewDialogue;

            // Connect
            ari.Connect();

            // Wait for keypress
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();

        }

        private static void AriOnNewDialogue(object sender, BrokerSession e)
        {
            Console.WriteLine("New Dialog Started: {0}", e.DialogueId);
            // A new application dialogue has been raised!
            // Events are now directly tied to a dialog via the BrokerSession
            e.OnStasisStartEvent += E_OnStasisStartEvent;
            e.OnStasisEndEvent += E_OnStasisEndEvent;
        }

        private static void E_OnStasisEndEvent(IAriClient sender, StasisEndEvent e)
        {
            
        }

        private static void E_OnStasisStartEvent(IAriClient sender, StasisStartEvent e)
        {
            // sender if BrokerSession which implements IAriClient
            sender.Channels.Answer(e.Channel.Id);
            sender.Channels.Play(e.Channel.Id, "sound:demo-congrats");
        }
    }
}
