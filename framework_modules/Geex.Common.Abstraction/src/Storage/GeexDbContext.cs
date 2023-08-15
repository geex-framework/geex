using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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
        public virtual async Task CommitAsync(CancellationToken? cancellation = default)
        {
            await SaveChanges(cancellation.GetValueOrDefault(CancellationToken.None));
            if (Session.IsInTransaction)
            {
                await Session.CommitTransactionAsync(cancellation.GetValueOrDefault(CancellationToken.None));
            }
            if (this.OnCommitted != default)
            {
                await this.OnCommitted();
            }
        }

        /// <inheritdoc />
        public event Func<Task>? OnCommitted;
    }
}
