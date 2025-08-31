using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

using MediatX.RabbitMQ;
using FastExpressionCompiler;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MediatX
{
    /// <summary>
    /// Provides extension methods for various classes.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Add the MediatX RabbitMQ message dispatcher to the service collection, allowing it to be resolved and used.
        /// </summary>
        /// <param name="services">The service collection to add the message dispatcher to.</param>
        /// <param name="config">The configuration settings for the message dispatcher.</param>
        /// <returns>The updated service collection.</returns>
        public static IServiceCollection AddMediatXRabbitMQ(this IServiceCollection services, Action<MessageDispatcherOptions> config)
        {
            services.Configure<MessageDispatcherOptions>(config);
            services.AddSingleton<IExternalMessageDispatcher, MessageDispatcher>(x => new MessageDispatcher(x.GetService<IOptions<MessageDispatcherOptions>>(), x.GetService<ILogger<MessageDispatcher>>()));
            services.AddHostedService<RequestsManager>();
            return services;
        }

        /// <summary>
        /// Computes the hash value of a string using the specified HashAlgorithm.
        /// </summary>
        /// <param name="input">The input string to be hashed.</param>
        /// <param name="hashAlgorithm">The HashAlgorithm to be used for computing the hash value.</param>
        /// <returns>
        /// The hexadecimal representation of the computed hash value.
        /// </returns>
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

        /// <summary>
        /// Computes the hash value for the specified byte array using the specified hashing algorithm.
        /// </summary>
        /// <param name="input">The input byte array to compute the hash for.</param>
        /// <param name="hashAlgorithm">The hashing algorithm to use.</param>
        /// <returns>
        /// A string representation of the computed hash value.
        /// </returns>
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

        private static readonly ConcurrentDictionary<MethodInfo, Func<object, object[], object>> _methodInvokerCache = new();
        /// <summary>
        /// 高性能普通方法调用
        /// </summary>
        internal static object InvokeFast(this MethodInfo method, object target, params object[] args)
        {
            var invoker = _methodInvokerCache.GetOrAdd(method, CreateMethodInvoker);
            return invoker(target, args);
        }
        private static Func<object, object[], object> CreateMethodInvoker(MethodInfo method)
        {
            var targetParam = Expression.Parameter(typeof(object), "target");
            var argsParam = Expression.Parameter(typeof(object[]), "args");

            var parameters = method.GetParameters();
            var argExpressions = new Expression[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var paramType = parameters[i].ParameterType;
                var argAccess = Expression.ArrayIndex(argsParam, Expression.Constant(i));
                argExpressions[i] = Expression.Convert(argAccess, paramType);
            }

            Expression callExpression;
            if (method.IsStatic)
            {
                callExpression = Expression.Call(method, argExpressions);
            }
            else
            {
                var targetCast = Expression.Convert(targetParam, method.DeclaringType);
                callExpression = Expression.Call(targetCast, method, argExpressions);
            }

            Expression resultExpression;
            if (method.ReturnType == typeof(void))
            {
                resultExpression = Expression.Block(callExpression, Expression.Constant(null));
            }
            else
            {
                resultExpression = Expression.Convert(callExpression, typeof(object));
            }

            var lambda = Expression.Lambda<Func<object, object[], object>>(
                resultExpression, targetParam, argsParam);

            return lambda.CompileFast();
        }
    }
}
