using System;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using Microsoft.Extensions.Logging;

namespace MediatX.Pipelines
{
    /// <summary>
    /// Represents a pipeline behavior that integrates with an mediatx for handling requests of type <typeparamref name="TRequest"/> and producing responses of type <typeparamref name
    /// ="TResponse"/>.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    public class MediatXPipeline<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> //where TRequest : notnull
    {
        private readonly IMediatX mediatx;
        private readonly ILogger<MediatX> _logger;

        public MediatXPipeline(IMediatX mediatx, ILogger<MediatX> logger)
        {
            this.mediatx = mediatx;
            _logger = logger;
        }

        // Implementation for legacy version for .netstandard 2.0 compatibility
        /// <summary>
        /// Handles a request asynchronously.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request.</typeparam>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="request">The request to be handled.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="next">The delegate to invoke the next handler in the pipeline.</param>
        /// <returns>A task representing the asynchronous handling of the request.</returns>
        /// <exception cref="InvalidHandlerException">Thrown when the handler location is invalid.</exception>
        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            try
            {
                if (request is IBaseRequest)
                {
                    return await next().ConfigureAwait(false);
                }

                if (request is INotification notification)
                {
                    await mediatx.SendRemoteNotification(notification);
                }

                throw new InvalidHandlerException();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        // Implementation for version > 11
        /// <summary>
        /// Handles the request by invoking the appropriate handler based on the location of the request.
        /// </summary>
        /// <typeparam name="TRequest">The type of request.</typeparam>
        /// <typeparam name="TResponse">The type of response.</typeparam>
        /// <param name="request">The request data.</param>
        /// <param name="next">The next request handler delegate.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The response data.</returns>
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            try
            {
                if (request is IBaseRequest)
                {
                    return await next().ConfigureAwait(false);
                }

                if (request is INotification notification)
                {
                    await mediatx.SendRemoteNotification(notification);
                }

                throw new InvalidHandlerException();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }
    }
}
