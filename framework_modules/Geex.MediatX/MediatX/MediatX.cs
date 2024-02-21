using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using MediatR;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MediatX
{
    /// <summary>
    /// Represents an mediatx that handles message routing and dispatching.
    /// </summary>
    public class MediatX : IMediatX
    {
        private readonly IExternalMessageDispatcher _messageDispatcher;
        private readonly ILogger<MediatX> _logger;

        public MediatX(IExternalMessageDispatcher messageDispatcher, ILogger<MediatX> logger)
        {
            this._messageDispatcher = messageDispatcher;
            this._logger = logger;
        }

        /// <summary>
        /// Sends a remote notification.
        /// </summary>
        /// <typeparam name="TRequest">The type of the notification.</typeparam>
        /// <param name="request">The notification request to send.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SendRemoteNotification<TRequest>(TRequest request) where TRequest : INotification
        {
            _logger.LogDebug($"Invoking remote handler for: {typeof(TRequest).TypeQueueName()}");
            await _messageDispatcher.Notify(request);
            _logger.LogDebug($"Remote request for {typeof(TRequest).TypeQueueName()} completed!");
        }
    }
}
