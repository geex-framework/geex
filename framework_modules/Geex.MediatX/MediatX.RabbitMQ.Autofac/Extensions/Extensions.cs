using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MediatX.RabbitMQ;
using Autofac;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MediatX
{
  public static class Extensions
  {
    public static ContainerBuilder AddMediatXRabbitMQMessageDispatcher(this ContainerBuilder services,
        Action<MessageDispatcherOptions> config,
        ILoggerFactory loggerFactory, JsonSerializerOptions jsonSerializerOptions)
    {
      var options = new MessageDispatcherOptions(jsonSerializerOptions);
      config(options);
      services.RegisterInstance(Options.Create(options)).SingleInstance();
      services.RegisterType<MessageDispatcher>().As<IExternalMessageDispatcher>().SingleInstance();

      services.RegisterInstance(LoggerFactoryExtensions.CreateLogger<MessageDispatcher>(loggerFactory)).As<ILogger<MessageDispatcher>>();
      services.RegisterInstance(LoggerFactoryExtensions.CreateLogger<RequestsManager>(loggerFactory)).As<ILogger<RequestsManager>>();
      return services;
    }

    public static ContainerBuilder ResolveMediatXCalls(this ContainerBuilder services)
    {
      services.RegisterType<RequestsManager>().AsSelf().As<IHostedService>().SingleInstance();
      return services;
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

    public static string GetHash(this byte[] input, HashAlgorithm hashAlgorithm)
    {
      byte[] data = hashAlgorithm.ComputeHash(input);
      var sBuilder = new StringBuilder();
      for (int i = 0; i < data.Length; i++)
      {
        sBuilder.Append(data[i].ToString("x2"));
      }

      return sBuilder.ToString();
    }
  }
}
