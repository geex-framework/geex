using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MediatX.GRPC.Extensions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Type = System.Type;

namespace MediatX.GRPC
{
  public class RequestsManager : global::MediatX.GRPC.GrpcServices.GrpcServicesBase
  {
    private readonly MediatXOptions _mediatxOptions;
    private readonly ILogger<RequestsManager> _logger;
    private readonly IServiceProvider _provider;
    private readonly MessageDispatcherOptions _options;

    private readonly HashSet<string> _deDuplicationCache = new HashSet<string>();
    private readonly SHA256 _hasher = SHA256.Create();

    private readonly Dictionary<string, Type> _typeMappings;

    public RequestsManager(IOptions<MessageDispatcherOptions> options, ILogger<RequestsManager> logger, IServiceProvider provider,
      IOptions<MediatXOptions> mediatxOptions)
    {
      this._mediatxOptions = mediatxOptions.Value;
      this._options = options.Value;
      this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
      this._provider = provider;

      _typeMappings = _mediatxOptions.LocalRequests.ToDictionary(k => k.TypeQueueName(_mediatxOptions), v => v);
    }


    public override async Task<MessageResponse> ManageMediatXMessage(RequestMessage request, ServerCallContext context)
    {
      _logger.LogDebug("Message Received. Looking for type");
      if (_typeMappings.TryGetValue(request.MediatXType, out var messageType))
      {
        var consumerMethod = typeof(RequestsManager)
          .GetMethod(nameof(RequestsManager.ManageGenericMediatXMessage), BindingFlags.Instance | BindingFlags.NonPublic)?
          .MakeGenericMethod(messageType);

        var response = await ((Task<string>)consumerMethod!.Invoke(this, new object[] { request }))!;
        return new MessageResponse() { Body = response };
      }

      return null;
    }

    public override async Task<Empty> ManageMediatXNotification(NotifyMessage request, ServerCallContext context)
    {
      _logger.LogDebug("Notification Received. Looking for type");
      if (_typeMappings.TryGetValue(request.MediatXType, out var messageType))
      {
        var consumerMethod = typeof(RequestsManager)
          .GetMethod(nameof(RequestsManager.ManageGenericMediatXNotification), BindingFlags.Instance | BindingFlags.NonPublic)?
          .MakeGenericMethod(messageType);

        await ((Task)consumerMethod!.Invoke(this, new object[] { request }))!;
      }

      return new Empty();
    }

    private async Task<string> ManageGenericMediatXMessage<T>(RequestMessage request)
    {
      string responseMsg = null;
      try
      {
        var msg = request.Body;
        _logger.LogDebug("Elaborating message : {0}", msg);
        var message = JsonSerializer.Deserialize<T>(msg, _options.SerializerSettings);


        var mediator = _provider.CreateScope().ServiceProvider.GetRequiredService<IMediator>();
        var response = await mediator.Send(message);
        responseMsg = JsonSerializer.Serialize(new Messages.ResponseMessage { Content = response, Status = Messages.StatusEnum.Ok },
          _options.SerializerSettings);
        _logger.LogDebug("Elaborating sending response : {0}", responseMsg);
      }
      catch (Exception ex)
      {
        responseMsg = JsonSerializer.Serialize(new Messages.ResponseMessage { Exception = ex, Status = Messages.StatusEnum.Exception },
          _options.SerializerSettings);
        _logger.LogError(ex, $"Error executing message of type {typeof(T)} from external service");
      }

      return responseMsg;
    }

    private async Task ManageGenericMediatXNotification<T>(NotifyMessage request)
    {
      var mediator = _provider.CreateScope().ServiceProvider.GetRequiredService<IMediator>();
      var mediatx = mediator as MediatXMediatr;
      try
      {
        var msg = request.Body;

        if (_options.DeDuplicationEnabled)
        {
          var hash = msg.GetHash(_hasher);
          lock (_deDuplicationCache)
            if (_deDuplicationCache.Contains(hash))
            {
              _logger.LogDebug($"duplicated message received : {request.MediatXType}");
              return;
            }

          lock (_deDuplicationCache)
            _deDuplicationCache.Add(hash);

          // Do not await this task
#pragma warning disable CS4014
          Task.Run(async () =>
          {
            await Task.Delay(_options.DeDuplicationTTL);
            lock (_deDuplicationCache)
              _deDuplicationCache.Remove(hash);
          });
#pragma warning restore CS4014
        }

        _logger.LogDebug("Elaborating notification : {0}", msg);
        var message = JsonSerializer.Deserialize<T>(msg, _options.SerializerSettings);

        mediatx?.StopPropagating();
        await mediator.Publish(message);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error executing message of type {typeof(T)} from external service");
      }
      finally
      {
        mediatx?.ResetPropagating();
      }
    }
  }
}
