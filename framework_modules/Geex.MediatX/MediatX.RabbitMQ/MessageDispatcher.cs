using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Threading;
using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.IO;

namespace MediatX.RabbitMQ
{
    /// <summary>
    /// Class for dispatching messages to RabbitMQ and handling responses.
    /// </summary>
    public class MessageDispatcher : IExternalMessageDispatcher, IDisposable
    {
        /// <summary>
        /// Represents the options for the message dispatcher.
        /// </summary>
        private readonly MessageDispatcherOptions options;

        /// <summary>
        /// The logger for the MessageDispatcher class.
        /// </summary>
        /// <typeparam name="MessageDispatcher">The type of the class that the logger is associated with.</typeparam>
        private readonly ILogger<MessageDispatcher> logger;

        /// <summary>
        /// Stores an instance of an object that implements the IConnection interface.
        /// </summary>
        private IConnection _connection = null;

        /// <summary>
        /// The channel used for sending messages.
        /// </summary>
        private IModel _sendChannel = null;

        /// <summary>
        /// Dictionary that maps callback strings to TaskCompletionSource objects.
        /// </summary>
        private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _callbackMapper =
          new ConcurrentDictionary<string, TaskCompletionSource<string>>();


        /// <summary>
        /// Constructor for the MessageDispatcher class. </summary> <param name="options">The options for the MessageDispatcher.</param> <param name="logger">The logger for the MessageDispatcher.</param>
        /// /
        public MessageDispatcher(
          IOptions<MessageDispatcherOptions> options,
          ILogger<MessageDispatcher> logger)
        {
            this.options = options.Value;
            this.logger = logger;

            this.InitConnection();
        }

        /// Initializes the RabbitMQ connection and sets up the necessary channels and consumers.
        /// /
        private void InitConnection()
        {
            // Ensuring we have a connetion object
            if (_connection == null)
            {
                logger.LogInformation($"Creating RabbitMQ Connection to '{options.HostName}'...");
                var factory = new ConnectionFactory
                {
                    HostName = options.HostName,
                    UserName = options.Username,
                    Password = options.Password,
                    VirtualHost = options.VirtualHost,
                    Port = options.Port,
                    DispatchConsumersAsync = true,
                };

                _connection = factory.CreateConnection();
            }

            _sendChannel = _connection.CreateModel();
        }

        /// <summary>
        /// Sends a notification message to the specified exchange and routing key.
        /// </summary>
        /// <typeparam name="TEvent">The type of the request message.</typeparam>
        /// <param name="routingKey"></param>
        /// <param name="request">The request message to send.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the notification operation.</param>
        /// <returns>A task representing the asynchronous notification operation.</returns>
        public async Task Notify<TEvent>(string routingKey, TEvent request, CancellationToken cancellationToken = default) where TEvent : IEvent
        {
            var fullRouteKey = Constants.MediatXExchangeName + "/" + routingKey;
            try
            {
                using var stream = new MemoryStream();
                await JsonSerializer.SerializeAsync(stream, request, options.SerializerSettings,
                    cancellationToken: cancellationToken);
                var message = stream.ToArray();

                logger.LogInformation("Sending message to: {fullRouteKey}", fullRouteKey);

                _sendChannel.BasicPublish(
                    exchange: Constants.MediatXExchangeName,
                    routingKey: routingKey,
                    mandatory: false,
                    body: message
                );
            }
            catch (Exception e)
            {
                logger.LogError("Failed to send mediator distributed event to [{routeKey}]: {event}", fullRouteKey, JsonSerializer.Serialize(request));
                throw;
            }
        }


        /// <summary>
        /// Retrieves the basic properties for a given correlation ID.
        /// </summary>
        /// <param name="correlationId">The correlation ID associated with the properties.</param>
        /// <returns>The basic properties object.</returns>
        private IBasicProperties GetBasicProperties(string correlationId)
        {
            var props = _sendChannel.CreateBasicProperties();
            props.CorrelationId = correlationId;
            return props;
        }

        /// <summary>
        /// Disposes of the resources used by the object.
        /// </summary>
        public void Dispose()
        {
            try
            {
                _sendChannel?.Close();
            }
            catch
            {
            }
        }
    }
}
