using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace MediatX
{
  /// <summary>
  /// Extension methods for configuring and using MediatX in an ASP.NET Core application.
  /// </summary>
  [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
  [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
  public static class MediatXExtensions
  {
    /// <summary>
    /// Adds the MediatX to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddMediatX(this IServiceCollection services)
    {
      services.AddScoped(typeof(IPipelineBehavior<,>), typeof(Pipelines.MediatXPipeline<,>));
      services.AddSingleton<IMediatX, MediatX>();
      services.AddTransient<IMediator, MediatXMediatr>();
      return services;
    }
    /// <summary>
    /// Gets the queue name for the specified type.
    /// </summary>
    /// <param name="t">The type.</param>
    /// <param name="sb">The <see cref="StringBuilder"/> instance to append the queue name to (optional).</param>
    /// <returns>The queue name for the specified type.</returns>
    public static string TypeQueueName(this Type t, StringBuilder sb = null)
    {
      if (t.CustomAttributes.Any())
      {
        var attr = t.GetCustomAttribute<MediatXQueueNameAttribute>();
        if (attr != null) return $"{t.Namespace}.{attr.Name}";
      }

      sb = sb ?? new StringBuilder();
      sb.Append($"{t.Namespace}.{t.Name}");

      if (t.GenericTypeArguments != null && t.GenericTypeArguments.Length > 0)
      {
        sb.Append("[");
        foreach (var ta in t.GenericTypeArguments)
        {
          ta.TypeQueueName(sb);
          sb.Append(",");
        }

        sb.Append("]");
      }

      return sb.ToString().Replace(",]", "]");
    }
  }
}
