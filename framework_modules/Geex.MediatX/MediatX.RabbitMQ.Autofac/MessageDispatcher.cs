using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Threading;
using System.Text;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using MediatX.Messages;

namespace MediatX.RabbitMQ
{
  public class MessageDispatcher : IExternalMessageDispatcher, IDisposable
  {
    private readonly MessageDispatcherOptions options;
    private readonly ILogger<MessageDispatcher> logger;
    private readonly MediatXOptions mediatxOptions;
    private IConnection _connection = null;
    private IModel _sendChannel = null;
    private string _replyQueueName = null;
    private AsyncEventingBasicConsumer _sendConsumer = null;
    private string _consumerId = null;

    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _callbackMapper =
      new ConcurrentDictionary<string, TaskCompletionSource<string>>();


    public MessageDispatcher(IOptions<MessageDispatcherOptions> options,
      ILogger<MessageDispatcher> logger, IOptions<MediatXOptions> mediatxOptions)
    {
      this.options = options.Value;
      this.logger = logger;
      this.mediatxOptions = mediatxOptions.Value;

      this.InitConnection();
    }

    private void InitConnection()
    {
      // Ensuring we have a connetion object
      if (_connection == null)
      {
        logger.LogInformation($"Creating RabbitMQ Connection to '{options.HostName}'...");
        var factory = new ConnectionFactory
        {
          HostName = options.HostName,
          UserName = options.UserName,
          Password = options.Password,
          VirtualHost = options.VirtualHost,
          Port = options.Port,
          DispatchConsumersAsync = true,
        };

        _connection = factory.CreateConnection();
      }

      _sendChannel = _connection.CreateModel();
      _sendChannel.ExchangeDeclare(Constants.MediatXExchangeName, ExchangeType.Topic);
      // _channel.ConfirmSelect();

      var queueName = $"{options.QueueName}.{Process.GetCurrentProcess().Id}.{DateTime.Now.Ticks}";
      _replyQueueName = _sendChannel.QueueDeclare(queue: queueName).QueueName;
      _sendConsumer = new AsyncEventingBasicConsumer(_sendChannel);
      _sendConsumer.Received += (s, ea) =>
      {
        if (!_callbackMapper.TryRemove(ea.BasicProperties.CorrelationId, out var tcs))
          return Task.CompletedTask;
        var body = ea.Body.ToArray();
        var response = Encoding.UTF8.GetString(body);
        tcs.TrySetResult(response);
        return Task.CompletedTask;
      };

      _sendChannel.BasicReturn += (s, ea) =>
      {
        if (!_callbackMapper.TryRemove(ea.BasicProperties.CorrelationId, out var tcs)) return;
        tcs.TrySetException(new Exception($"Unable to deliver required action: {ea.RoutingKey}"));
      };

      this._consumerId = _sendChannel.BasicConsume(queue: _replyQueueName, autoAck: true, consumer: _sendConsumer);
    }


    public async Task<Messages.ResponseMessage<TResponse>> Dispatch<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
    {
      var message = JsonSerializer.Serialize<TRequest>(request, options.SerializerSettings);

      var correlationId = Guid.NewGuid().ToString();

      var tcs = new TaskCompletionSource<string>();
      _callbackMapper.TryAdd(correlationId, tcs);

      _sendChannel.BasicPublish(
        exchange: Constants.MediatXExchangeName,
        routingKey: typeof(TRequest).TypeQueueName(mediatxOptions),
        mandatory: true,
        body: Encoding.UTF8.GetBytes(message),
        basicProperties: GetBasicProperties(correlationId));

      cancellationToken.Register(() => _callbackMapper.TryRemove(correlationId, out var tmp));
      var result = await tcs.Task;

      return JsonSerializer.Deserialize<Messages.ResponseMessage<TResponse>>(result, options.SerializerSettings);
    }

    public Task Notify<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : INotification
    {
      var message = JsonSerializer.Serialize(request, options.SerializerSettings);

      logger.LogInformation($"Sending message to: {Constants.MediatXExchangeName}/{request.GetType().TypeQueueName(mediatxOptions)}");

      _sendChannel.BasicPublish(
        exchange: Constants.MediatXExchangeName,
        routingKey: request.GetType().TypeQueueName(mediatxOptions),
        mandatory: false,
        body: Encoding.UTF8.GetBytes(message)
      );

      return Task.CompletedTask;
    }


    private IBasicProperties GetBasicProperties(string correlationId)
    {
      var props = _sendChannel.CreateBasicProperties();
      props.CorrelationId = correlationId;
      props.ReplyTo = _replyQueueName;
      return props;
    }

    public void Dispose()
    {
      try
      {
        _sendChannel?.BasicCancel(_consumerId);
        _sendChannel?.Close();
        _connection.Close();
      }
      catch
      {
      }
    }
  }
}
