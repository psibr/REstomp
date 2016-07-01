# reSTOMP
THE STOMP v1.2 solution on .NET Core 1.0 and .NET4.5+

[![Build status](https://ci.appveyor.com/api/projects/status/arpll9rcl5x0i6t6/branch/master?svg=true)](https://ci.appveyor.com/project/psibernetic/restomp/branch/master)

## What is STOMP
"STOMP is the Simple (or Streaming) Text Orientated Messaging Protocol.
STOMP provides an interoperable wire format so that STOMP clients can communicate
with any STOMP message broker to provide easy and widespread messaging interoperability 
among many languages, platforms and brokers." - [STOMP Home page](https://stomp.github.io)

## Why this library

STOMP brokers are actually quite common, for instance both ApacheMQ and RabbitMQ
are interoperable with STOMP in addition to proprietary formats and the complicated 
AMQP ~~spec~~.

However, I am not currently familiar with any .NET brokers, **a damn shame.**

We are building this not as a broker server for .NET, but instead as a set of 
libraries to enable working with STOMP in .NET in an effortless manner.

- StompParser - A stream capable STOMP v1.2 frame parser and serializer.
- reSTOMP - A server-as-a-library solution similar to OWIN for STOMP connections 
 

 The STOMP specification is a liberal specification for server implmentation, 
 which allows us to *hopefully* build a system of reSTOMP middleware for controlling 
 advanced broker features such as:

 - Message Persistance
 - Authentication
 - Authorization
 - Routing
 - Heart-Beats (keep-alives)
 - Queuing


 ## Why Now

 With .NET moving cross-platform and the emergence of micro-service environments, 
 one thing missing is a consistent mode of inter-communication. 
 HTTP is todays de-facto standard on .NET with other Service Bus options often 
 getting complicated or requiring a heavy dependence. 
 The aim of reSTOMP is to minimize the effort and topological complexity of a messaging 
 system.

 One primary design goal for this project is enabling **each** micro-service to self-host 
 a STOMP broker and perform direct communication instead of a centrallized broker. 
 This feels more in line with the distributed mentality.