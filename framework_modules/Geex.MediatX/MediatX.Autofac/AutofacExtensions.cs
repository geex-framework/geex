using System;
using Autofac;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MediatX
{
  public static class AutofacExtensions
  {
    /// <summary>
    /// Adds the MediatX services to the ContainerBuilder. </summary> <param name="builder">The ContainerBuilder to add the services to.</param> <param name="configure">The action used to configure the MediatXOptions.</param> <param name="loggerFactory">The ILoggerFactory used to create loggers.</param> <returns>The modified ContainerBuilder.</returns>
    /// /
    public static ContainerBuilder AddMediatX(this ContainerBuilder builder, Action<MediatXOptions> configure, ILoggerFactory loggerFactory)
    {
      if (configure != null)
      {
        var options = new MediatXOptions();
        configure(options);
        var opt = Options.Create<MediatXOptions>(options);

        builder.RegisterInstance(opt).SingleInstance();
      }

      builder.RegisterType<MediatX>().As<IMediatX>().SingleInstance();
      builder.RegisterType<MediatXMediatr>().As<IMediator>();
      builder.RegisterGeneric(typeof(Pipelines.MediatXPipeline<,>)).As(typeof(IPipelineBehavior<,>));

      builder.RegisterInstance(LoggerFactoryExtensions.CreateLogger<MediatXMediatr>(loggerFactory)).As<ILogger<MediatXMediatr>>();
      builder.RegisterInstance(LoggerFactoryExtensions.CreateLogger<MediatX>(loggerFactory)).As<ILogger<MediatX>>();

      return builder;
    }
  }
}
