using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Geex.Common.Abstractions;
using Geex.MongoDB.Entities.Utilities;

using KellermanSoftware.CompareNetObjects;

using MediatR;

using Microsoft.Extensions.DependencyInjection;

using MongoDB.Driver;
using MongoDB.Entities;

using Nito.AsyncEx.Synchronous;

using Volo.Abp;

using BusinessException = Geex.Common.Abstractions.BusinessException;

namespace Geex.Common.Abstraction.Storage
{
    public class GeexDbContext : DbContext, IUnitOfWork
    {
        static GeexDbContext()
        {
            DbContext._compareLogic.Config.CustomComparers.Add(new EnumerationComparer(RootComparerFactory.GetRootComparer()));
            DbContext._compareLogic.Config.CustomComparers.Add(new GeexByteArrayComparer(RootComparerFactory.GetRootComparer()));
        }
        public GeexDbContext(IServiceProvider serviceProvider = default, string database = default,
            bool transactional = false,
            ClientSessionOptions options = null, bool entityTrackingEnabled = true) : base(serviceProvider, database, transactional, options, entityTrackingEnabled)
        {

        }

        public override T Attach<T>(T entity)
        {
            if (Equals(entity, default(T)))
            {
                return default;
            }

            if (entity is IEntity geexEntity)
            {
                if (entity.Id.IsNullOrEmpty())
                {
                    this.DomainEvents.Enqueue(new EntityCreatedNotification<T>((T)(object)geexEntity));
                    entity = base.Attach(entity);
                    // todo: 区分innerAttach和外部attach, innerAttach不进行校验逻辑
#pragma warning disable CS0618
                    geexEntity.Validate().WaitAndUnwrapException();
#pragma warning restore CS0618
                }
                else
                {
                    entity = base.Attach(entity);
                }

            }
            else
            {
                entity = base.Attach(entity);
            }

            return entity;
        }

        public override IEnumerable<T> Attach<T>(IEnumerable<T> entities)
        {
            foreach (var entity in entities)
            {
                this.Attach(entity);
            }

            return entities;
        }
        [Obsolete("do not use event, use direct method or event publish instead")]
        public Queue<INotification> DomainEvents { get; } = new Queue<INotification>();

        /// <inheritdoc />
        public override async Task<int> SaveChanges(CancellationToken cancellation = default)
        {
            var mediator = ServiceProvider.GetService<IMediator>();
            if (this.DomainEvents.Any())
            {
                while (this.DomainEvents.TryDequeue(out var @event))
                {
                    await mediator?.Publish(@event, cancellation);
                }
            }

            var entities = Local.TypedCacheDictionary.Values.SelectMany(y => y.Values).OfType<IEntity>();
            foreach (var entity in entities)
            {
                entity.Validate().WaitAndUnwrapException(cancellation);
            }
            return await base.SaveChanges(cancellation);
        }

        public TResult RawCommand<TResult>(Command<TResult> command, ReadPreference readPreference = default,
            CancellationToken cancellationToken = default)
        {
            return DB.DefaultDb.RunCommand(this.Session, command, readPreference, cancellationToken);
        }


        /// <summary>
        /// Commits a transaction to MongoDB
        /// </summary>
        /// <param name="cancellation">An optional cancellation token</param>
        public override async Task CommitAsync(CancellationToken cancellation = default)
        {
            await base.CommitAsync(cancellation);
        }

        /// <inheritdoc />
        public async Task AbortAsync(CancellationToken cancellationToken = default)
        {
            if (Session.IsInTransaction)
            {
                await Session.AbortTransactionAsync(cancellationToken);
                return;
            }
            throw new InvalidOperationException("session not in transaction, cannot abort");
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync<T>(string id, CancellationToken cancellation = default) where T : IEntityBase
        {
            var result = await base.DeleteAsync<T>(id, cancellation);
            return result.IsAcknowledged;
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync<T>(T entity, CancellationToken cancellation = default) where T : IEntityBase
        {
            var result = await base.DeleteAsync<T>(entity, cancellation);
            return result.IsAcknowledged;
        }

        /// <inheritdoc />
        public async Task<long> DeleteAsync<T>(Expression<Func<T, bool>> expression, CancellationToken cancellation = default) where T : IEntityBase
        {
            var result = await base.DeleteAsync<T>(expression, cancellation);
            return result.DeletedCount;
        }

        /// <inheritdoc />
        public async Task<long> DeleteAsync<T>(CancellationToken cancellation = default) where T : IEntityBase
        {
            var result = await base.DeleteAsync<T>(cancellation);
            return result.DeletedCount;
        }

        /// <inheritdoc />
        public async Task<long> DeleteAsync<T>(IEnumerable<string> ids, CancellationToken cancellation = default) where T : IEntityBase
        {
            var result = await base.DeleteAsync<T>(ids, cancellation);
            return result.DeletedCount;
        }

        /// <inheritdoc />
        public override event Func<Task>? OnCommitted;

        /// <inheritdoc />
        public IQueryable<T> Query<T>() where T : IEntityBase
        {
            return base.Query<T>();
        }
    }
}
