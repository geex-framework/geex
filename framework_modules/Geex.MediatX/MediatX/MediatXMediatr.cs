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
    public class MediatXMediatr : Mediator
    {
        private readonly IMediatX _mediatx;
        private readonly ILogger<MediatXMediatr> _logger;
        private bool _allowRemoteRequest = true;

        public MediatXMediatr(IServiceProvider serviceProvider, IMediatX mediatx, ILogger<MediatXMediatr> logger) : base(serviceProvider)
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
        /// <param name="notification">The notification to publish.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override async Task PublishCore(IEnumerable<NotificationHandlerExecutor> handlerExecutors, MediatR.INotification notification,
          CancellationToken cancellationToken)
        {
            try
            {
                var localHandlerExecutors = handlerExecutors.Where(x => x.HandlerInstance is not IRemoteNotificationHandler);
                if (_allowRemoteRequest)
                {
                    if (notification is IRemoteNotification remoteNotification)
                    {
                        var remoteHandlerExecutors = handlerExecutors.Where(x => x.HandlerInstance is IRemoteNotificationHandler);
                        if (remoteHandlerExecutors.Any())
                        {
                            _logger.LogDebug("Propagating: {Json}", JsonSerializer.Serialize(remoteNotification));
                            await _mediatx.SendRemoteNotification(remoteNotification);
                        }
                    }
                    else
                    {
                        await base.PublishCore(localHandlerExecutors, notification, cancellationToken);
                    }
                }
                else
                {
                    if (notification is IRemoteNotification)
                    {
                        throw new InvalidOperationException("remote notification is not enabled, please setup GeexCoreModuleOptions.RabbitMq to enable it.");
                    }
                    await base.PublishCore(localHandlerExecutors, notification, cancellationToken);
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
