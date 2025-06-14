using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;

using MediatR;

using Microsoft.Extensions.DependencyInjection;

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
            services.AddSingleton<IMediator, Mediator>();
            services.AddSingleton<MediatXMediatr>();
            return services;
        }

        /// <summary>
        /// Gets the queue name for the specified type.
        /// </summary>
        /// <param name="handlerType">The type.</param>
        /// <param name="sb">The <see cref="StringBuilder"/> instance to append the queue name to (optional).</param>
        /// <returns>The queue name for the specified type.</returns>
        public static string TypeQueueName(this Type handlerType, StringBuilder sb = null)
        {
            if (handlerType.CustomAttributes.Any())
            {
                var attr = handlerType.GetCustomAttribute<MediatXQueueNameAttribute>();
                if (attr != null) return $"{handlerType.Namespace}.{attr.Name}";
            }

            sb = sb ?? new StringBuilder();
            sb.Append($"{handlerType.Namespace}.{handlerType.Name}");

            if (handlerType.GenericTypeArguments != null && handlerType.GenericTypeArguments.Length > 0)
            {
                sb.Append("[");
                foreach (var ta in handlerType.GenericTypeArguments)
                {
                    ta.TypeQueueName(sb);
                    sb.Append(",");
                }

                sb.Append("]");
            }

            return sb.ToString().Replace(",]", "]");
        }

        /// <summary>
        /// Gets the queue name for the specified type.
        /// </summary>
        /// <param name="messageType">The type.</param>
        /// <param name="sb">The <see cref="StringBuilder"/> instance to append the queue name to (optional).</param>
        /// <returns>The queue name for the specified type.</returns>
        public static string TypeRouteKey(this Type messageType, StringBuilder sb = null)
        {
            if (messageType.CustomAttributes.Any())
            {
                var attr = messageType.GetCustomAttribute<MediatXQueueNameAttribute>();
                if (attr != null) return $"{messageType.Namespace}.{attr.Name}";
            }

            sb = sb ?? new StringBuilder();
            if (!messageType.IsGenericType)
                return messageType.FullName;

            var name = messageType.FullName.Substring(0, messageType.FullName.IndexOf('`'));
            var args = string.Join("],[", messageType.GetGenericArguments().Select(x => x.TypeRouteKey(sb)));

            sb.Append(name);
            sb.Append("`");
            sb.Append(messageType.GetGenericArguments().Length);
            sb.Append("[[");
            sb.Append(args);
            sb.Append("]]");

            return sb.ToString();
        }
    }
}
