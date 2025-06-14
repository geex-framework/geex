using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Geex.Common;

using MediatR;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MediatX
{
    /// <summary>
    /// Represents an mediatx that handles message routing and dispatching.
    /// </summary>
    public class Mediator : MediatR.Mediator, IMediator
    {
        private readonly IExternalMessageDispatcher _messageDispatcher;
        private readonly ILogger<Mediator> _logger;

        public Mediator(IServiceProvider serviceProvider, ILogger<Mediator> logger, bool enableDistributedEvent = false) : base(serviceProvider)
        {
            if (enableDistributedEvent)
            {
                this._messageDispatcher = serviceProvider.GetService<IExternalMessageDispatcher>();
            }
            this._logger = logger;
        }

        /// <summary>
        /// Sends a distributed event.
        /// </summary>
        /// <typeparam name="TEvent">The type of the event.</typeparam>
        /// <param name="event">The event request to send.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task PublishDistributedEvent<TEvent>(TEvent @event) where TEvent : IDistributedEvent
        {
            if (_messageDispatcher == default)
            {
                throw new InvalidOperationException("Distributed event handling is not enabled. Please configure GeexCoreModuleOptions.RabbitMq to enable it.");
            }
            // todo: here need simulate contexts or distinguish the remote request
            var routingKey = typeof(TEvent).TypeRoutingKey();
            _logger.LogDebug("PublishDistributedEvent [{routingKey}] started.", routingKey);
            await _messageDispatcher.Notify(routingKey, @event);
            _logger.LogDebug("PublishDistributedEvent [{routingKey}] finished.", routingKey);
        }

        /// <summary>
        /// Publishes the given notification by invoking the registered notification handlers.
        /// </summary>
        /// <param name="handlerExecutors">The notification handler executors.</param>
        /// <param name="event">The notification to publish.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override async Task PublishCore(IEnumerable<NotificationHandlerExecutor> handlerExecutors, MediatR.INotification @event,
          CancellationToken cancellationToken)
        {
            try
            {
                var localHandlerExecutors = handlerExecutors.Where(x => x.HandlerInstance is not IDistributedEventHandler);
                if (@event is IDistributedEvent distributedEvent)
                {
                    var distributedHandlerExecutors = handlerExecutors.Where(x => x.HandlerInstance is IDistributedEventHandler);
                    if (!distributedHandlerExecutors.Any())
                    {
                        _logger.LogWarning("No distributed event handler found for event type {EventType}.", @event.GetType().FullName);
                    }
                    await this.PublishDistributedEvent(distributedEvent);
                }
                else
                {
                    await base.PublishCore(localHandlerExecutors, @event, cancellationToken);
                }
                //if (@event is IDistributedEvent)
                //{
                //    throw new InvalidOperationException("remote notification is not enabled, please setup GeexCoreModuleOptions.RabbitMq to enable it.");
                //}
                //await base.PublishCore(localHandlerExecutors, @event, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }
    }
}
