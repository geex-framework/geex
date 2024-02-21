# MediatX
[![NuGet](https://img.shields.io/nuget/dt/mediatx.svg)](https://www.nuget.org/packages/mediatx) 
[![NuGet](https://img.shields.io/nuget/vpre/mediatx.svg)](https://www.nuget.org/packages/mediatx)


MediatX provides [MediatR](https://github.com/jbogard/MediatR) Pipelines to transform mediator from In-process to Out-Of-Process messaging via RPC calls implemented with popular message dispatchers. 

## When you need MediatX. 

Mediatr is very good to implement some patterns like [CQRS](https://docs.microsoft.com/en-us/azure/architecture/patterns/cqrs)

Implementing CQRS in an in-process application does not bring to you all the power of the pattern but gives you the opportunity to have organized, easy to maintain code. When the application grow you may need to refactor your things to a microservices architecture.

Microservices and patterns like CQRS are very powerfull combination. In this scenario you will need to rewrite communication part to use some kind of out of process message dispatcher.

MediatX change Mediatr behaviour and let you decide which call needs to be in-process and which needs to be Out-of-process and dispatched remotely, via a configuration without changing a single row of your code.


## Versioning

MediatX follow MediatR versioning.

   
 ```
 MediatX v.12.x is for MediatR from 12.x
 MediatX v.9.0 is for MediatR from 9.x to 11.x
 ```
   
 

## Installation

You should install [MediatX with NuGet](https://www.nuget.org/packages/mediatx):

    Install-Package MediatX
    
Or via the .NET Core command line interface:

    dotnet add package MediatX

Either commands, from Package Manager Console or .NET Core CLI, will download and install MediatX and all required dependencies.


## Basic Configuration 

Configuring MediatX is an easy task. 
1) Add MediatX to services configuration via AddMediatX extension method. 

``` 
  services.AddMediatX(opt => ...
```

2) Decide what is the default behaviour, available options are 
   1) ***ImplicitLocal*** : all `mediator.Send()` calls will be delivered in-process unless further configuration. 
   2) ***ImplicitRemote*** : all `mediator.Send()` calls will be delivered out-of-process unless further configuration. 
   3) ***Explicit*** : you have the responsability do declare how to manage every single call. 


```
    services.AddMediatX(opt =>
    {
      opt.Behaviour = MediatXBehaviourEnum.Explicit;
    });
```


3) Configure calls delivery type according with you behaviour:

```
    services.AddMediatX(opt =>
    {
      opt.Behaviour = MediatXBehaviourEnum.Explicit;
      opt.SetAsRemoteRequest<MediatRRequest1>();
      opt.SetAsRemoteRequest<MediatRRequest2>();
      ....
    }
```


Of course you will have some processes with requests declared **Local** and other processes with same requests declared **Remote**. 


### Example of process with all local calls and some remote calls

```
    services.AddMediatX(opt =>
    {
      opt.Behaviour = MediatXBehaviourEnum.ImplicitLocal;
      opt.SetAsRemoteRequest<MediatRRequest1>();
      opt.SetAsRemoteRequest<MediatRRequest2>();
      opt.SetAsRemoteRequests(typeof(MediatRRequest2).Assembly); // All requests in an assembly
    });
```


### Example of process with local handlers. 

```
    services.AddMediatX(opt =>
    {
      opt.Behaviour = MediatXBehaviourEnum.ImplicitLocal;
    });

```

### Example of process with remore handlers. 

```
    services.AddMediatX(opt =>
    {
      opt.Behaviour = MediatXBehaviourEnum.ImplicitRemote;
    });
```


# MediatX with RabbitMQ


## Installing MediatX RabbitMQ extension.

```
    Install-Package MediatX.RabbitMQ
```
    
Or via the .NET Core command line interface:

```
    dotnet add package MediatX.RabbitMQ
```

## Configuring RabbitMQ Extension. 

Once installed you need to configure rabbitMQ extension. 

```
    services.AddMediatXRabbitMQMessageDispatcher(opt =>
    {
      opt.HostName = "rabbit instance";
      opt.Port = 5672;
      opt.Password = "password";
      opt.UserName = "rabbituser";
      opt.VirtualHost = "/";
    });
    services.ResolveMediatXCalls();
```

or if you prefer use appsettings configuration 

```
    services.AddMediatXRabbitMQMessageDispatcher(opt => context.Configuration.GetSection("rabbitmq").Bind(opt));
    services.ResolveMediatXCalls();
```


# MediatX with Kafka

## Installing MediatX Kafka extension.

```
    Install-Package MediatX.Kafka
```
    
Or via the .NET Core command line interface:

```
    dotnet add package MediatX.Kafka
```


## Configuring Kafka Extension. 

Once installed you need to configure Kafka extension. 

```
    services.AddMediatXKafkaMessageDispatcher(opt =>
    {
      opt.BootstrapServers = "localhost:9092";
    });
    services.ResolveMediatXCalls();
```

or if you prefer use appsettings configuration 

```
    services.AddMediatXKafkaMessageDispatcher(opt => context.Configuration.GetSection("kafka").Bind(opt));
    services.ResolveMediatXCalls();
```



# MediatX with Azure Message Queues

Coming soon. 
