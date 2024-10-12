using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using Geex.Common.Abstraction.ClientNotification;
using Geex.Common.Abstraction.Storage;
using Geex.Common.ClientNotification;

using HotChocolate.Subscriptions;

using MediatR;

using Microsoft.Extensions.DependencyInjection;

using MongoDB.Entities;

namespace Geex.Common;

public interface IBus
{
    public IMediator Mediator { get; }

    /// <summary>Asynchronously send a request to a single handler</summary>
    /// <typeparam name="TResponse">Response type</typeparam>
    /// <param name="request">Request object</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>A task that represents the send operation. The task result contains the handler response</returns>
    Task<TResponse> Request<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default(CancellationToken)) =>
        Mediator.Send(request, cancellationToken);

    /// <summary>
    /// Asynchronously send a request to a single handler with no response
    /// </summary>
    /// <param name="request">Request object</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>A task that represents the send operation.</returns>
    Task Request<TRequest>(TRequest request, CancellationToken cancellationToken = default(CancellationToken)) where TRequest : IRequest => Mediator.Send(request, cancellationToken);

    /// <summary>
    /// Asynchronously send an object request to a single handler via dynamic dispatch
    /// </summary>
    /// <param name="request">Request object</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>A task that represents the send operation. The task result contains the type erased handler response</returns>
    Task<object?> Request(object request, CancellationToken cancellationToken = default(CancellationToken)) => Mediator.Send(request, cancellationToken);

    /// <summary>Asynchronously send a notification to multiple handlers</summary>
    /// <param name="notification">Notification object</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>A task that represents the publish operation.</returns>
    Task Notify(object notification, CancellationToken cancellationToken = default(CancellationToken)) => Mediator.Publish(notification, cancellationToken);

    /// <summary>Asynchronously send a notification to multiple handlers</summary>
    /// <param name="notification">Notification object</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>A task that represents the publish operation.</returns>
    Task Notify<TNotification>(TNotification notification, CancellationToken cancellationToken = default(CancellationToken)) where TNotification : INotification => Mediator.Publish(notification, cancellationToken);
}

public interface IRepository
{
    IQueryable<T> Query<T>() where T : IEntityBase;
    IServiceProvider ServiceProvider { get; }
}
public interface IUnitOfWork : IRepository, IBus, IDisposable
{
    GeexDbContext DbContext => this as GeexDbContext;
    public event Func<Task>? PreSaveChanges;
    public event Func<Task>? PostSaveChanges;
    T Attach<T>(T entity) where T : IEntityBase;
    T AttachNoTracking<T>(T entity) where T : IEntityBase;
    IEnumerable<T> Attach<T>(IEnumerable<T> entities) where T : IEntityBase;

    async Task ClientNotify<T>(T clientNotify, params string[] userIds) where T : ClientNotify
    {
        var topicEventSender = this.ServiceProvider.GetService<ITopicEventSender>();
        if (!userIds.IsNullOrEmpty())
        {
            await Task.WhenAll(userIds.Select(userId => topicEventSender.SendAsync($"{nameof(ClientNotifySubscription.OnPrivateNotify)}:{userId}", clientNotify).AsTask()));
        }
        else
        {
            await topicEventSender.SendAsync(nameof(ClientNotifySubscription.OnPublicNotify), clientNotify as ClientNotify);
        }
    }

    /// <inheritdoc />
    Task<List<string>> SaveChanges(CancellationToken cancellation = default);

    public Task<bool> DeleteAsync<T>(string id, CancellationToken cancellation = default)
            where T : IEntityBase;

    public Task<bool> DeleteAsync<T>(T entity, CancellationToken cancellation = default)
        where T : IEntityBase;

    /// <summary>
    /// Deletes matching entities from MongoDB in the transaction scope
    /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
    /// <para>TIP: Try to keep the number of entities to delete under 100 in a single call</para>
    /// </summary>
    /// <typeparam name="T">The type of entity</typeparam>
    /// <param name="expression">A lambda expression for matching entities to delete.</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task<long> DeleteAsync<T>(Expression<Func<T, bool>> expression,
        CancellationToken cancellation = default) where T : IEntityBase;

    public Task<long> DeleteAsync<T>(CancellationToken cancellation = default) where T : IEntityBase;

    /// <summary>
    /// Deletes matching entities from MongoDB in the transaction scope
    /// <para>HINT: If these entities are referenced by one-to-many/many-to-many relationships, those references are also deleted.</para>
    /// <para>TIP: Try to keep the number of entities to delete under 100 in a single call</para>
    /// </summary>
    /// <typeparam name="T">The type of entity</typeparam>
    /// <param name="ids">An IEnumerable of entity Ids</param>
    /// <param name="cancellation">An optional cancellation token</param>
    public Task<long> DeleteAsync<T>(IEnumerable<string> ids,
        CancellationToken cancellation = default) where T : IEntityBase;
}