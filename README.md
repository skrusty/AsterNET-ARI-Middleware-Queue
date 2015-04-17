# AsterNET-ARI-Middleware-Queue
A queue middleware implementation for AsterNET.ARI allowing you to use AsterNET.ARI with Ari Proxies like go-ari-proxy.

## Overview
Middleware.Queue allows one or many ARI proxies to communicate with your AsterNET.ARI application. This provides both High Availability and Load Balancing.

The Middleware Queue provides it's own client AriBrokerClient which allows you to connect to a queue implementation, currently RabbitMq is supported. The AriBrokerClient raises an event when a new Dialogue is created by the proxy and returns you a handler (BrokerSession, implements IAriClient) that allows you to talk back to the originating proxy and receive Stasis events.

### Communications Overview
![image](https://www.dropbox.com/s/jg45yzsswzn4h9w/ari_middleware_interfaces_azure_%20(4).png?dl=1)

## Ari Proxies
Middleware.Queue has been tested with [go-ari-proxy](https://github.com/nvisibleinc/go-ari-proxy).

## AriClientBroker
The AriClientBroker allows you to establish a connection with a queue provider (e.g. RabbitMq) and receive new dialogues. When a new dialogue is received, AriBrokerClient establishes the requires sub queues or topics to handle the new dialogue. It rases the OnNewDialogue event and passes an instance of BrokerSession (which implements IAriClient).

```c#
var ari = new AriBrokerClient("queue-name",
  new RabbitMq("amqp://", new RabbitMqOptions()
  {
      AutoDelete = false,
      Durable = true,
      Exclusive = false
  }));

// Hook into AriOnNewDialogue event
ari.OnNewDialogue += AriOnNewDialogue;

// Connect
ari.Connect();
```

## Dialogues
A dialogue encapsulates all the communications required for an ARI Application and a proxy to communicate. Each dialogue includes an event queue and a request and response queue. These are exposed as an implementation of IAriClient.

```c#
private static void AriOnNewDialogue(object sender, BrokerSession e)
{
    Console.WriteLine("New Dialogue Started: {0}", e.DialogueId);
    // A new application dialogue has been raised!
    // Events are now directly tied to a dialogue via the BrokerSession
    e.OnStasisStartEvent += E_OnStasisStartEvent;
    e.OnStasisEndEvent += E_OnStasisEndEvent;
}
```

## Work as Normal
```c#
private static void E_OnStasisStartEvent(IAriClient sender, StasisStartEvent e)
{
    // sender if BrokerSession which implements IAriClient
    sender.Channels.Answer(e.Channel.Id);
    sender.Channels.Play(e.Channel.Id, "sound:demo-congrats");
}
```
