using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Geex.Common;

using MediatR;

using Microsoft.Extensions.Logging;

namespace MediatX
{
    /// MediatXMediatr class is a subclass of Mediator that adds additional functionality for remote request arbitration.
    /// /
    public class MediatXMediatr : MediatR.Mediator
    {
        private readonly IMediator _mediatx;
        private readonly ILogger<MediatXMediatr> _logger;
        private bool _allowRemoteRequest = true;

        public MediatXMediatr(IServiceProvider serviceProvider, IMediator mediatx, ILogger<MediatXMediatr> logger) : base(serviceProvider)
        {
            this._mediatx = mediatx;
            this._logger = logger;
        }

        /// <summary>
        /// Stops the propagation of remote requests.
        /// </summary>
        public void StopPropagating()
        {
            _allowRemoteRequest = false;
        }

        /// <summary>
        /// Resets the propagating state to allow remote requests.
        /// </summary>
        public void ResetPropagating()
        {
            _allowRemoteRequest = true;
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
                if (_allowRemoteRequest)
                {
                    if (@event is IDistributedEvent remoteNotification)
                    {
                        var remoteHandlerExecutors = handlerExecutors.Where(x => x.HandlerInstance is IDistributedEventHandler);
                        if (remoteHandlerExecutors.Any())
                        {
                            _logger.LogDebug("SendDistributedEvent: {Json}", JsonSerializer.Serialize(remoteNotification));
                            await _mediatx.SendDistributedEvent(remoteNotification);
                        }
                    }
                    else
                    {
                        await base.PublishCore(localHandlerExecutors, @event, cancellationToken);
                    }
                }
                else
                {
                    if (@event is IDistributedEvent)
                    {
                        throw new InvalidOperationException("remote notification is not enabled, please setup GeexCoreModuleOptions.RabbitMq to enable it.");
                    }
                    await base.PublishCore(localHandlerExecutors, @event, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }
    }
}
