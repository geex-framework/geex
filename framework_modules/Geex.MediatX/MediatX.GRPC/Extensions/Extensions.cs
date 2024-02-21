using System;
using System.Security.Cryptography;
using System.Text;
using MediatX.GRPC;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MediatX.GRPC.Extensions
{
  public static class Extensions
  {
    /// <summary>
    /// Add the MediatX RabbitMQ message dispatcher to the service collection, allowing it to be resolved and used.
    /// </summary>
    /// <param name="services">The service collection to add the message dispatcher to.</param>
    /// <param name="config">The configuration settings for the message dispatcher.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddMediatXGrpcDispatcher(this IServiceCollection services, Action<MessageDispatcherOptions> config)
    {
      services.AddGrpc();
      services.Configure<MessageDispatcherOptions>(config);
      services.AddSingleton<IExternalMessageDispatcher, MessageDispatcher>();

      return services;
    }

    public static IEndpointRouteBuilder UseGrpcDispatcher(this IEndpointRouteBuilder host)
    {
      host.MapGrpcService<RequestsManager>();
      return host;
    }

    public static string GetHash(this string input, HashAlgorithm hashAlgorithm)
    {
      byte[] data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));
      var sBuilder = new StringBuilder();

      for (int i = 0; i < data.Length; i++)
      {
        sBuilder.Append(data[i].ToString("x2"));
      }

      return sBuilder.ToString();
    }
  }
}
