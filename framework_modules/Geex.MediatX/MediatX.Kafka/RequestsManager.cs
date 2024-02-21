using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka.Admin;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace MediatX.Kafka
{
  /// <summary>
  /// Represents a class that manages requests and notifications using Kafka.
  /// </summary>
  public class RequestsManager : IHostedService
  {
    private readonly ILogger<RequestsManager> _logger;
    private readonly IMediatX _mediatx;
    private readonly IServiceProvider _provider;
    private readonly MediatXOptions _mediatxOptions;

    private readonly MessageDispatcherOptions _options;
    private IProducer<Null, string> _producer;
    private IConsumer<Null, string> _requestConsumer;
    private IConsumer<Null, string> _notificationConsumer;

    private Thread _notificationConsumerThread;
    private Thread _requestConsumerThread;

    private readonly Dictionary<string, MethodInfo> _methods = new Dictionary<string, MethodInfo>();

    public RequestsManager(ILogger<RequestsManager> logger, IOptions<MessageDispatcherOptions> options, IMediatX mediatx, IServiceProvider provider,
      IOptions<MediatXOptions> mediatxOptions)
    {
      _logger = logger;
      _options = options.Value;
      this._mediatx = mediatx;
      this._provider = provider;
      _mediatxOptions = mediatxOptions.Value;
    }

    /// <summary>
    /// Starts the asynchronous process of connecting to Kafka and subscribing to messages.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
      _producer = new ProducerBuilder<Null, string>(this._options.GetProducerConfig()).Build();
      _logger.LogInformation($"Creating Kafka Connection to '{_options.BootstrapServers}'...");

      {
        var config = this._options.GetConsumerConfig();
        config.GroupId = "MediatX";
        _requestConsumer = new ConsumerBuilder<Null, string>(config).Build();
      }

      {
        var config = this._options.GetConsumerConfig();
        config.GroupId = $"Notification.{Process.GetCurrentProcess().Id}.{DateTime.Now.Ticks}";
        _notificationConsumer = new ConsumerBuilder<Null, string>(config).Build();
      }

      var notificationsSubscriptions = new List<string>();
      var requestSubscriptions = new List<string>();

      foreach (var t in _mediatx.GetLocalRequestsTypes())
      {
        if (t is null) continue;
        _provider.CreateTopicAsync(_options, t.TypeQueueName(_mediatxOptions));
        var isNotification = typeof(INotification).IsAssignableFrom(t);

        if (isNotification)
        {
          notificationsSubscriptions.Add(t.TypeQueueName(_mediatxOptions));
          var consumerMethod = typeof(RequestsManager)
            .GetMethod("ConsumeChannelNotification", BindingFlags.Instance | BindingFlags.NonPublic)?
            .MakeGenericMethod(t);
          _methods.Add(t.TypeQueueName(_mediatxOptions), consumerMethod);
        }
        else
        {
          requestSubscriptions.Add(t.TypeQueueName(_mediatxOptions));
          var consumerMethod = typeof(RequestsManager)
            .GetMethod("ConsumeChannelMessage", BindingFlags.Instance | BindingFlags.NonPublic)?
            .MakeGenericMethod(t);
          _methods.Add(t.TypeQueueName(_mediatxOptions), consumerMethod);
        }
      }

      _requestConsumer.Subscribe(requestSubscriptions);
      _notificationConsumer.Subscribe(notificationsSubscriptions);

      _requestConsumerThread = new Thread(() =>
      {
        while (true)
        {
          var notification = _requestConsumer.Consume();
          _methods.TryGetValue(notification.Topic, out var method);
          if (method != null)
            method.Invoke(this, new object[] { notification.Message.Value });
        }
      }) { IsBackground = true };
      _requestConsumerThread.Start();

      _notificationConsumerThread = new Thread(() =>
      {
        while (true)
        {
          var notification = _notificationConsumer.Consume();
          _methods.TryGetValue(notification.Topic, out var method);
          if (method != null)
            method.Invoke(this, new object[] { notification.Message.Value });
        }
      }) { IsBackground = true };

      _notificationConsumerThread.Start();


      return Task.CompletedTask;
    }

    /// <summary>
    /// Elaborates and processes a notification received from a channel. </summary> <typeparam name="T">The type of the message.</typeparam> <param name="msg">The notification message to be processed.</param> <returns>A task representing the asynchronous operation.</returns>
    /// /
    private async Task ConsumeChannelNotification<T>(string msg)
    {
      _logger.LogDebug("Elaborating notification : {Msg}", msg);
      var message = JsonSerializer.Deserialize<KafkaMessage<T>>(msg, _options.SerializerSettings);
      if (message == null)
      {
        _logger.LogError("Unable to deserialize message {Msg}", msg);
        return;
      }

      var mediator = _provider.CreateScope().ServiceProvider.GetRequiredService<IMediator>();
      try
      {
        var mediatx = mediator as MediatXMediatr;
        mediatx?.StopPropagating();
        await mediator.Publish(message.Message);
        mediatx?.ResetPropagating();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error executing message of type {typeof(T)} from external service");
      }
    }

    /// <summary>
    /// Consume a channel message and process it.
    /// </summary>
    /// <typeparam name="T">Type of the message content.</typeparam>
    /// <param name="msg">The message to consume.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ConsumeChannelMessage<T>(string msg)
    {
      _logger.LogDebug("Elaborating message : {Msg}", msg);
      var message = JsonSerializer.Deserialize<KafkaMessage<T>>(msg, _options.SerializerSettings);

      if (message == null)
      {
        _logger.LogError("Unable to deserialize message {Msg}", msg);
        return;
      }

      var mediator = _provider.CreateScope().ServiceProvider.GetRequiredService<IMediator>();
      string responseMsg = null;
      try
      {
        var response = await mediator.Send(message.Message);
        responseMsg = JsonSerializer.Serialize(
          new KafkaReply
          {
            Reply = new Messages.ResponseMessage { Content = response, Status = Messages.StatusEnum.Ok },
            CorrelationId = message.CorrelationId
          }, _options.SerializerSettings);
        _logger.LogDebug("Elaborating sending response : {Msg}", responseMsg);
      }
      catch (Exception ex)
      {
        responseMsg = JsonSerializer.Serialize(
          new KafkaReply()
          {
            Reply = new Messages.ResponseMessage { Exception = ex, Status = Messages.StatusEnum.Exception },
            CorrelationId = message.CorrelationId
          }
          , _options.SerializerSettings);
        _logger.LogError(ex, $"Error executing message of type {typeof(T)} from external service");
      }
      finally
      {
        await _producer.ProduceAsync(message.ReplyTo, new Message<Null, string>() { Value = responseMsg });
      }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
      return Task.CompletedTask;
    }
  }
}
