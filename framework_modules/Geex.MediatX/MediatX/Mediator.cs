using System;
using System.Threading.Tasks;

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

        public Mediator(IServiceProvider serviceProvider, IExternalMessageDispatcher messageDispatcher, ILogger<Mediator> logger) : base(serviceProvider)
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
        public async Task SendDistributedEvent<TRequest>(TRequest request) where TRequest : IDistributedEvent
        {
            // todo: here need simulate contexts or distinguish the remote request
            _logger.LogDebug($"Invoking remote handler for: {typeof(TRequest).TypeRouteKey()}");
            await _messageDispatcher.Notify(request);
            _logger.LogDebug($"Remote request for {typeof(TRequest).TypeRouteKey()} completed!");
        }
    }
}
