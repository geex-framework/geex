using MediatR;

namespace sender;

public class Worker : BackgroundService
{
  private readonly ILogger<Worker> _logger;

  private readonly IServiceProvider provider;

  public Worker(ILogger<Worker> logger, IServiceProvider provider)
  {
    _logger = logger;
    this.provider = provider;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    var mediatr = provider.CreateScope().ServiceProvider.GetRequiredService<IMediator>();
    while (!stoppingToken.IsCancellationRequested)
    {
      _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

      try
      {
        var response = await mediatr.Send(new cqrs.models.Commands.MediatRRequest1(), stoppingToken);
        _logger.LogInformation($"Operation succeded with {response}");
      }
      catch (Exception ex)
      {
        _logger.LogError(exception: ex, "Error in remote request");
      }
      
      try
      {
        var response = await mediatr.Send(new cqrs.models.Commands.MediatRRequest2(), stoppingToken);
        _logger.LogInformation($"Operation succeded with {response}");
      }
      catch (Exception ex)
      {
        _logger.LogError(exception: ex, "Error in remote request");
      }
      
      try
      {
        var response = await mediatr.Send(new cqrs.models.Commands.MediatRRequest3(), stoppingToken);
        _logger.LogInformation($"Operation succeded with {response}");
      }
      catch (Exception ex)
      {
        _logger.LogError(exception: ex, "Error in remote request");
      }
      
      try
      {
        var response = await mediatr.Send(new cqrs.models.Commands.MediatRRequest4(), stoppingToken);
        _logger.LogInformation($"Operation succeded with {response}");
      }
      catch (Exception ex)
      {
        _logger.LogError(exception: ex, "Error in remote request");
      }
      
      try
      {
        var response = await mediatr.Send(new cqrs.models.Commands.MediatRRequest5(), stoppingToken);
        _logger.LogInformation($"Operation succeded with {response}");
      }
      catch (Exception ex)
      {
        _logger.LogError(exception: ex, "Error in remote request");
      }
      
      
      // try
      // {
      //   var response = await mediatr.Send(new cqrs.models.Commands.MediatRRequestWithHandlerException(), stoppingToken);
      //   _logger.LogInformation($"Operation succeded with {response}");
      // }
      // catch (Exception ex)
      // {
      //   _logger.LogError(exception: ex, "Error in remote request MediatRRequestWithHandlerException");
      // }
      
      // try
      // {
      //   var response = await mediatr.Send(new cqrs.models.Commands.MediatRRequestWithException(), stoppingToken);
      //   _logger.LogInformation($"Operation succeded with {response}");
      // }
      // catch (Exception ex)
      // {
      //   _logger.LogError(exception: ex, "Error in remote request MediatRRequestWithException");
      // }


      // try
      // {
      //   await mediatr.Publish(new cqrs.models.Commands.MediatorNotification1(), stoppingToken);
      //   _logger.LogInformation($"Operation succeded ");
      // }
      // catch (Exception ex)
      // {
      //   _logger.LogError(exception: ex, "Error in remote request MediatRRequestWithNoHandlers");
      // }

      // try
      // {
      //   var response = await mediatr.Send(new cqrs.models.Commands.MediatRRequestWithNoHandlers(), stoppingToken);
      //   _logger.LogInformation($"Operation succeded with {response}");
      // }
      // catch (Exception ex)
      // {
      //   _logger.LogError(exception: ex, "Error in remote request MediatRRequestWithNoHandlers");
      // }


      await Task.Delay(100, stoppingToken);
    }
  }
}