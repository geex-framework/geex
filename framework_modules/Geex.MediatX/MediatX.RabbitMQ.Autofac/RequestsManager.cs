using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Core.Lifetime;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MediatX.RabbitMQ
{
  public class RequestsManager : IHostedService
  {
    private readonly ILogger<RequestsManager> _logger;
    private readonly IMediatX _mediatx;
    private readonly ILifetimeScope _provider;
    private readonly MediatXOptions _mediatxOptions;

    private IConnection _connection = null;
    private IModel _channel = null;

    private readonly HashSet<string> _deDuplicationCache = new HashSet<string>();
    private readonly SHA256 _hasher = SHA256.Create();

    private readonly MessageDispatcherOptions _options;

    public RequestsManager(IOptions<MessageDispatcherOptions> options, ILogger<RequestsManager> logger,
      IMediatX mediatx, ILifetimeScope provider, IOptions<MediatXOptions> mediatxOptions)
    {
      this._options = options.Value;
      this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
      this._mediatx = mediatx;
      this._provider = provider;
      _mediatxOptions = mediatxOptions.Value;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
      if (_connection == null)
      {
        _logger.LogInformation($"ARBITRER: Creating RabbitMQ Connection to '{_options.HostName}'...");
        var factory = new ConnectionFactory
        {
          HostName = _options.HostName,
          UserName = _options.UserName,
          Password = _options.Password,
          VirtualHost = _options.VirtualHost,
          Port = _options.Port,
          DispatchConsumersAsync = true,
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(Constants.MediatXExchangeName, ExchangeType.Topic);

        _logger.LogInformation("ARBITRER: ready !");
      }


      foreach (var t in _mediatx.GetLocalRequestsTypes())
      {
        if (t is null) continue;
        var isNotification = typeof(INotification).IsAssignableFrom(t);
        var queueName = $"{t.TypeQueueName(_mediatxOptions)}${(isNotification ? Guid.NewGuid().ToString() : "")}";

        _channel.QueueDeclare(queue: queueName, durable: _options.Durable, exclusive: isNotification, autoDelete: _options.AutoDelete, arguments: null);
        _channel.QueueBind(queueName, Constants.MediatXExchangeName, t.TypeQueueName(_mediatxOptions));


        var consumer = new AsyncEventingBasicConsumer(_channel);

        var consumerMethod = typeof(RequestsManager)
          .GetMethod(isNotification ? "ConsumeChannelNotification" : "ConsumeChannelMessage", BindingFlags.Instance | BindingFlags.NonPublic)
          ?.MakeGenericMethod(t);

        consumer.Received += async (s, ea) =>
        {
          try
          {
            if (consumerMethod != null)
              await (Task)consumerMethod.Invoke(this, new object[] { s, ea });
          }
          catch (Exception e)
          {
            _logger.LogError(e, e.Message);
            throw;
          }
        };
        _channel.BasicConsume(queue: queueName, autoAck: isNotification, consumer: consumer);
      }

      _channel.BasicQos(0, 1, false);
      return Task.CompletedTask;
    }

    private async Task ConsumeChannelNotification<T>(object sender, BasicDeliverEventArgs ea)
    {
      var msg = ea.Body.ToArray();

      if (_options.DeDuplicationEnabled)
      {
        var hash = msg.GetHash(_hasher);
        lock (_deDuplicationCache)
          if (_deDuplicationCache.Contains(hash))
          {
            _logger.LogDebug($"duplicated message received : {ea.Exchange}/{ea.RoutingKey}");
            return;
          }

        lock (_deDuplicationCache)
          _deDuplicationCache.Add(hash);

        // Do not await this task
#pragma warning disable CS4014
        Task.Run(async () =>
        {
          await Task.Delay(_options.DeDuplicationTTL);
          lock (_deDuplicationCache)
            _deDuplicationCache.Remove(hash);
        });
#pragma warning restore CS4014
      }

      _logger.LogDebug("Elaborating notification : {0}", Encoding.UTF8.GetString(msg));
      var message = JsonSerializer.Deserialize<T>(Encoding.UTF8.GetString(msg), _options.SerializerSettings);

      var replyProps = _channel.CreateBasicProperties();
      replyProps.CorrelationId = ea.BasicProperties.CorrelationId;
      try
      {
        if (!_provider.BeginLifetimeScope(MatchingScopeLifetimeTags.RequestLifetimeScopeTag).TryResolve<IMediator>(out var mediator))
          mediator = _provider.BeginLifetimeScope().Resolve<IMediator>();

        var mediatx = mediator as MediatXMediatr;
        mediatx?.StopPropagating();
        await mediator.Publish(message);
        mediatx?.ResetPropagating();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error executing message of type {typeof(T)} from external service");
      }
    }

    private async Task ConsumeChannelMessage<T>(object sender, BasicDeliverEventArgs ea)
    {
      var msg = ea.Body.ToArray();
      _logger.LogDebug("Elaborating message : {0}", Encoding.UTF8.GetString(msg));
      var message = JsonSerializer.Deserialize<T>(Encoding.UTF8.GetString(msg), _options.SerializerSettings);

      var replyProps = _channel.CreateBasicProperties();
      replyProps.CorrelationId = ea.BasicProperties.CorrelationId;
      string responseMsg = null;
      try
      {
        if (!_provider.BeginLifetimeScope(MatchingScopeLifetimeTags.RequestLifetimeScopeTag).TryResolve<IMediator>(out var mediator))
          mediator = _provider.BeginLifetimeScope().Resolve<IMediator>();

        var response = await mediator.Send(message);
        responseMsg = JsonSerializer.Serialize(new Messages.ResponseMessage { Content = response, Status = Messages.StatusEnum.Ok },
          _options.SerializerSettings);
        _logger.LogDebug("Elaborating sending response : {0}", responseMsg);
      }
      catch (Exception ex)
      {
        responseMsg = JsonSerializer.Serialize(new Messages.ResponseMessage { Exception = ex, Status = Messages.StatusEnum.Exception },
          _options.SerializerSettings);
        _logger.LogError(ex, $"Error executing message of type {typeof(T)} from external service");
      }
      finally
      {
        _channel.BasicPublish(exchange: "", routingKey: ea.BasicProperties.ReplyTo, basicProperties: replyProps,
          body: Encoding.UTF8.GetBytes(responseMsg ?? ""));
        _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
      }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
      try
      {
        _channel?.Close();
      }
      catch
      {
        // ignored
      }

      try
      {
        _connection?.Close();
      }
      catch
      {
        // ignored
      }

      return Task.CompletedTask;
    }
  }
}
