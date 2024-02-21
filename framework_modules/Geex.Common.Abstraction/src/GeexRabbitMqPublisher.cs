//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using MediatR;

//using Microsoft.Extensions.Options;

//using RabbitMQ.Client.Events;

//using RabbitMQ.Client;
//using Microsoft.Extensions.Logging;
//using System.Collections.Concurrent;
//using System.Text.Json;
//using Geex.Common.Abstraction;
//using System.Linq;
//using System.Reflection;
//using MediatX;

//namespace Geex.Common.Abstractions;

//public class GeexRabbitMqPublisher : INotificationPublisher
//{
//    const string ExchangeName = "Geex-Notify-Exchange";
//    /// <summary>
//    /// Represents the options for the message dispatcher.
//    /// </summary>
//    private readonly GeexCoreModuleOptions options;

//    /// <summary>
//    /// The logger for the GeexRabbitMqPublisher class.
//    /// </summary>
//    /// <typeparam name="GeexRabbitMqPublisher">The type of the class that the logger is associated with.</typeparam>
//    private readonly ILogger<GeexRabbitMqPublisher> logger;

//    /// <summary>
//    /// Stores an instance of an object that implements the IConnection interface.
//    /// </summary>
//    private IConnection _connection = null;

//    /// <summary>
//    /// The channel used for sending messages.
//    /// </summary>
//    private IModel _sendChannel = null;

//    /// <summary>
//    /// Represents the name of the reply queue.
//    /// </summary>
//    private string _replyQueueName = null;

//    /// <summary>
//    /// Represents an asynchronous event-based consumer for sending messages.
//    /// </summary>
//    private AsyncEventingBasicConsumer _sendConsumer = null;

//    /// <summary>
//    /// The unique identifier of the consumer.
//    /// </summary>
//    private string _consumerId = null;

//    /// <summary>
//    /// Dictionary that maps callback strings to TaskCompletionSource objects.
//    /// </summary>
//    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _callbackMapper =
//      new ConcurrentDictionary<string, TaskCompletionSource<string>>();


//    /// <summary>
//    /// Constructor for the GeexRabbitMqPublisher class. </summary> <param name="options">The options for the GeexRabbitMqPublisher.</param> <param name="logger">The logger for the GeexRabbitMqPublisher.</param>
//    /// /
//    public GeexRabbitMqPublisher(
//      IOptions<GeexCoreModuleOptions> options,
//      ILogger<GeexRabbitMqPublisher> logger)
//    {
//        this.options = options.Value;
//        this.logger = logger;

//        this.InitConnection();
//    }

//    public async Task Publish(
//        IEnumerable<NotificationHandlerExecutor> handlerExecutors,
//        INotification notification,
//        CancellationToken cancellationToken)
//    {
//        foreach (var handlerExecutor in handlerExecutors)
//        {
//            await handlerExecutor.HandlerCallback(notification, cancellationToken).ConfigureAwait(false);
//            await this.Notify(notification, cancellationToken);
//        }
//    }

//    /// Initializes the RabbitMQ connection and sets up the necessary channels and consumers.
//    /// /
//    private void InitConnection()
//    {
//        // Ensuring we have a connetion object
//        var mqOptions = options.RabbitMq;
//        if (_connection == null)
//        {
//            logger.LogInformation($"Creating RabbitMQ Connection to '{mqOptions.HostName}'...");
//            var factory = new ConnectionFactory
//            {
//                HostName = mqOptions.HostName,
//                UserName = mqOptions.Username,
//                Password = mqOptions.Password,
//                VirtualHost = mqOptions.VirtualHost,
//                Port = mqOptions.Port,
//                DispatchConsumersAsync = true,
//            };

//            _connection = factory.CreateConnection();
//        }
//        _sendChannel = _connection.CreateModel();
//        _sendChannel.ExchangeDeclare(ExchangeName, ExchangeType.Topic);
//        // _channel.ConfirmSelect();

//        _replyQueueName = _sendChannel.QueueDeclare($"{options.AppName}.{Process.GetCurrentProcess().Id}.{DateTime.Now.Ticks}").QueueName;
//        _sendConsumer = new AsyncEventingBasicConsumer(_sendChannel);
//        _sendConsumer.Received += (s, ea) =>
//        {
//            TaskCompletionSource<string> tcs = null;
//            try
//            {
//                if (!_callbackMapper.TryRemove(ea.BasicProperties.CorrelationId, out tcs))
//                    return Task.CompletedTask;


//                var body = ea.Body.ToArray();
//                var response = Encoding.UTF8.GetString(body);
//                tcs.TrySetResult(response);
//            }
//            catch (Exception ex)
//            {
//                logger.LogError($"Error deserializing response: {ex.Message}", ex);
//                tcs?.TrySetException(ex);
//            }
//            return Task.CompletedTask;
//        };

//        _sendChannel.BasicReturn += (s, ea) =>
//        {
//            if (!_callbackMapper.TryRemove(ea.BasicProperties.CorrelationId, out var tcs)) return;
//            tcs.TrySetException(new Exception($"Unable to deliver required action: {ea.RoutingKey}"));
//        };

//        this._consumerId = _sendChannel.BasicConsume(queue: _replyQueueName, autoAck: true, consumer: _sendConsumer);
//    }

//    /// <summary>
//    /// Sends a notification message to the specified exchange and routing key.
//    /// </summary>
//    /// <typeparam name="TRequest">The type of the request message.</typeparam>
//    /// <param name="request">The request message to send.</param>
//    /// <param name="cancellationToken">A cancellation token to cancel the notification operation.</param>
//    /// <returns>A task representing the asynchronous notification operation.</returns>
//    public Task Notify<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : INotification
//    {
//        var message = JsonSerializer.Serialize(request, System.Text.Json.Json.DefaultSerializeSettings);

//        logger.LogInformation($"Sending message to: {ExchangeName}/{TypeQueueName(request.GetType())}");

//        _sendChannel.BasicPublish(
//          exchange: ExchangeName,
//          routingKey: TypeQueueName(request.GetType()),
//          mandatory: false,
//          body: Encoding.UTF8.GetBytes(message)
//        );

//        return Task.CompletedTask;
//    }

//    /// <summary>
//    /// Gets the queue name for the specified type.
//    /// </summary>
//    /// <param name="t">The type.</param>
//    /// <param name="sb">The <see cref="StringBuilder"/> instance to append the queue name to (optional).</param>
//    /// <returns>The queue name for the specified type.</returns>
//    public string TypeQueueName(Type t, StringBuilder sb = null)
//    {
//        if (t.CustomAttributes.Any())
//        {
//            var attr = t.GetCustomAttribute<MediatXQueueNameAttribute>();
//            if (attr != null) return $"{t.Namespace}.{attr.Name}".Replace(".", "_");
//        }

//        // var prefix = options.DefaultQueuePrefix;
//        sb ??= new StringBuilder();

//        sb.Append($"{t.Namespace}.{t.Name}");

//        if (t.GenericTypeArguments != null && t.GenericTypeArguments.Length > 0)
//        {
//            sb.Append("[");
//            foreach (var ta in t.GenericTypeArguments)
//            {
//                TypeQueueName(ta, sb);
//                sb.Append(",");
//            }

//            sb.Append("]");
//        }

//        return sb.ToString().Replace(",]", "]").Replace(".", "_");
//    }
//}