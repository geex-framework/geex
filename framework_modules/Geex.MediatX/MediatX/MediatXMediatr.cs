using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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
    protected override async Task PublishCore(IEnumerable<NotificationHandlerExecutor> handlerExecutors, INotification notification,
      CancellationToken cancellationToken)
    {
      try
      {
        if (_allowRemoteRequest)
        {
          _logger.LogDebug("Propagating: {Json}", JsonSerializer.Serialize(notification));
          await _mediatx.SendRemoteNotification(notification);
        }
        else
          await base.PublishCore(handlerExecutors, notification, cancellationToken);
      }      catch (Exception ex)
      {
        _logger.LogError(ex, ex.Message);
        throw;
      }
    }
  }
}
