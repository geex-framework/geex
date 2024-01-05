using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using MongoDB.Driver;
using MongoDB.Entities;

namespace Geex.Common;

public interface IRepository
{
    IQueryable<T> Query<T>() where T : IEntityBase;
    IServiceProvider ServiceProvider { get; }
}
public interface IUnitOfWork : IRepository, IDisposable
{
    public event Func<Task>? PostSaveChanges;
    T Attach<T>(T entity) where T : IEntityBase;
    IEnumerable<T> Attach<T>(IEnumerable<T> entities) where T : IEntityBase;

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