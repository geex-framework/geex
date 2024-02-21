using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Text;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Text.Json;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.VisualBasic;

namespace MediatX.GRPC
{
  /// <summary>
  /// Class for dispatching messages to RabbitMQ and handling responses.
  /// </summary>
  public class MessageDispatcher : IExternalMessageDispatcher, IDisposable
  {
    private readonly MessageDispatcherOptions options;
    private readonly ILogger<MessageDispatcher> logger;

    private readonly MediatXOptions mediatxOptions;

    public MessageDispatcher(
      IOptions<MessageDispatcherOptions> options,
      ILogger<MessageDispatcher> logger, IOptions<MediatXOptions> mediatxOptions)
    {
      this.options = options.Value;
      this.logger = logger;
      this.mediatxOptions = mediatxOptions.Value;
    }


    public Dictionary<string, GrpcChannel> DestinationChannels { get; set; } = new();

    public GrpcChannel GetChannelFor<T>()
    {
      if (this.options.RemoteTypeServices.TryGetValue(typeof(T), out var service))
      {
        if (!DestinationChannels.ContainsKey(service.Uri))
          DestinationChannels.Add(service.Uri, GrpcChannel.ForAddress(service.Uri, service.ChannelOptions));

        return DestinationChannels[service.Uri];
      }

      return null;
    }


    public async Task<Messages.ResponseMessage<TResponse>> Dispatch<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
    {
      var message = JsonSerializer.Serialize(request, options.SerializerSettings);

      var grpcClient = new GrpcServices.GrpcServicesClient(this.GetChannelFor<TRequest>());
      var result = await grpcClient.ManageMediatXMessageAsync(new RequestMessage
      {
        Body = message,
        MediatXType = typeof(TRequest).TypeQueueName(mediatxOptions)
      });
      return JsonSerializer.Deserialize<Messages.ResponseMessage<TResponse>>(result.Body, options.SerializerSettings);
    }

    /// <summary>
    /// Sends a notification message to the specified exchange and routing key.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request message.</typeparam>
    /// <param name="request">The request message to send.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the notification operation.</param>
    /// <returns>A task representing the asynchronous notification operation.</returns>
    public Task Notify<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : INotification
    {
      var message = JsonSerializer.Serialize(request, options.SerializerSettings);

      logger.LogInformation($"Sending notifications of: {typeof(TRequest).Name}/{request.GetType().TypeQueueName(mediatxOptions)}");

      foreach (var channel in DestinationChannels)
      {
        var grpcClient = new GrpcServices.GrpcServicesClient(channel.Value);
        grpcClient.ManageMediatXNotificationAsync(new NotifyMessage()
        {
          Body = message,
          MediatXType = typeof(TRequest).TypeQueueName(mediatxOptions)
        });
      }

      return Task.CompletedTask;
    }

    public void Dispose()
    {

    }
  }
}
