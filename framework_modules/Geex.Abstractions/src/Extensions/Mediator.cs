using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading;

using MediatX;

using MongoDB.Entities;

// ReSharper disable once CheckNamespace
namespace Mediator
{
    public static class MediatorExtensions
    {
        /// <summary>
        /// 映射缓存
        /// </summary>
        static ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> mapDictionary = new();

        static Task<TResponse> Send<TRequest, TResponse>(
            this IMediator sender,
            TRequest request,
            CancellationToken cancellationToken = default(CancellationToken)) where TRequest : IRequest<TResponse>
        {
            return sender.Send(request, cancellationToken);
        }
    }
}
