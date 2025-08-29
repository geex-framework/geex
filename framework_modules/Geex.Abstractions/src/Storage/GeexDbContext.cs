using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using Geex.Abstractions;
using Geex.Migrations;
using Geex.MongoDB.Entities.Utilities;
using Geex.Notifications;

using KellermanSoftware.CompareNetObjects;

using MediatX;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Entities;

using Nito.AsyncEx.Synchronous;

namespace Geex.Storage
{
    public class GeexDbContext : DbContext, IUnitOfWork
    {
        static GeexDbContext()
        {
            //DbContext._compareLogic.Config.CustomComparers.Add(new EnumerationComparer(RootComparerFactory.GetRootComparer()));
            //DbContext._compareLogic.Config.CustomComparers.Add(new GeexByteArrayComparer(RootComparerFactory.GetRootComparer()));
            DbContext.saveMethod = typeof(GeexCommonAbstractionStorageExtensions).GetMethods().First(x => x.Name == nameof(GeexCommonAbstractionStorageExtensions.SaveAsync) && x.GetParameters().First().ParameterType.Name.Contains("IEnumerable"));
        }

        /// <inheritdoc />
        public IMediator Mediator { get; }
        public GeexDbContext(IServiceProvider serviceProvider = default, string database = default,
            ClientSessionOptions options = null, bool entityTrackingEnabled = true) : base(serviceProvider, database, options, entityTrackingEnabled)
        {
            Mediator = serviceProvider.GetService<IMediator>();
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
                    this.DomainEvents.Enqueue(new EntityCreatedEvent<T>((T)geexEntity));
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
            return entities.Select(this.Attach).ToList();
        }
        public Queue<IEvent> DomainEvents { get; } = new Queue<IEvent>();

        /// <inheritdoc />
        public override async Task<List<string>> SaveChanges(CancellationToken cancellation = default)
        {
            var logger = ServiceProvider.GetService<ILogger<GeexDbContext>>();

            var entities = MemoryDataCache.TypedCacheDictionary.Values.SelectMany(y => y.Values).OfType<IEntity>();
            foreach (var entity in entities)
            {
                entity.ValidateOnAttach().WaitAndUnwrapException(cancellation);
            }

            if (this.DomainEvents.Any())
            {
                while (this.DomainEvents.TryDequeue(out var @event))
                {
                    try
                    {
                        await this.Mediator?.Publish(@event, cancellation);
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "Domain event failed to process: {event}", @event.ToJson());
                    }
                }
            }

            var result = await base.SaveChanges(cancellation);

            return result;
        }

        public TResult RawCommand<TResult>(Command<TResult> command, ReadPreference readPreference = default,
            CancellationToken cancellationToken = default)
        {
            return DB.DefaultDb.RunCommand(this.Session, command, readPreference, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync<T>(string id, CancellationToken cancellation = default) where T : IEntityBase
        {
            var result = await base.DeleteAsync<T>(id, cancellation);
            if (result > 0)
            {
                this.DomainEvents.Enqueue(new EntityDeletedEvent<T>(id));
                return true;
            }
            return false;
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync<T>(T entity, CancellationToken cancellation = default) where T : IEntityBase
        {
            var result = await base.DeleteAsync<T>(entity, cancellation);
            if (result > 0)
            {
                this.DomainEvents.Enqueue(new EntityDeletedEvent<T>(entity.Id));
                return true;
            }
            return false;
        }

        /// <inheritdoc />
        public async Task<long> DeleteAsync<T>(Expression<Func<T, bool>> expression, CancellationToken cancellation = default) where T : IEntityBase
        {
            ThrowIfCancellationNotSupported(cancellation);

            long deletedCount = 0;

            var cursor = await new Find<T, string>(this)
                               .Match(expression)
                               .Project(e => e.Id)
                               .Option(o => o.BatchSize = 100000)
                               .ExecuteCursorAsync(cancellation)
                               .ConfigureAwait(false);

            using (cursor)
            {
                while (await cursor.MoveNextAsync(cancellation).ConfigureAwait(false))
                {
                    if (cursor.Current.Any())
                    {
                        var toDeletes = cursor.Current;
                        var batchResult = await this.DeleteAsync<T>(toDeletes, cancellation);
                        deletedCount += batchResult;
                    }
                }
            }

            return deletedCount;
        }

        /// <inheritdoc />
        public async Task<long> DeleteAsync<T>(CancellationToken cancellation = default) where T : IEntityBase
        {
            var result = await base.DeleteAsync<T>(cancellation);
            return result;
        }

        /// <inheritdoc />
        public async Task<long> DeleteAsync<T>(IEnumerable<string> ids, CancellationToken cancellation = default) where T : IEntityBase
        {
            foreach (var id in ids)
            {
                this.DomainEvents.Enqueue(new EntityDeletedEvent<T>(id));
            }
            var result = await base.DeleteAsync<T>(ids, cancellation);
            return result;
        }

        /// <inheritdoc />
        public IQueryable<T> Query<T>() where T : IEntityBase
        {
            return base.Query<T>();
        }

        //protected internal virtual async Task MigrateAsync(DbMigration migration)
        //{
        //    var sw = new Stopwatch();
        //    // 默认的Session超时太短, 给Migration更多的超时时间
        //    var migrationName = migration.GetType().Name;
        //    var mig = new Migration
        //    {
        //        Number = migration.Number,
        //        Name = migrationName,
        //        TimeTakenSeconds = sw.Elapsed.TotalSeconds
        //    };
        //    sw.Start();
        //    this.session.StartTransaction(DefaultSessionOptions.DefaultTransactionOptions);
        //    await migration.UpgradeAsync(this).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
        //    this.Attach(mig);
        //    await SaveChanges();
        //    await this.session.CommitTransactionAsync();
        //    sw.Stop();
        //    sw.Reset();
        //}

        protected internal virtual async Task MigrateAsync(DbMigration migration)
        {
            var sw = new Stopwatch();
            // 默认的Session超时太短, 给Migration更多的超时时间
            var migrationName = migration.GetType().Name;

            sw.Start();
            await migration.UpgradeAsync(this);
            sw.Stop();
            await SaveChanges();
            var mig = new Migration
            {
                Number = migration.Number,
                Name = migrationName,
                TimeTakenSeconds = sw.Elapsed.TotalSeconds
            };
            this.Attach(mig);
            await SaveChanges();
        }

        public T Create<T>()
        {
            return ActivatorUtilities.CreateInstance<T>(ServiceProvider);
        }
    }
    public static class ActivatorUtilitiesExtensions
    {
        public static T CreateInstance<T>(this IServiceProvider serviceProvider, params object[] parameters)
        {
            return ActivatorUtilities.CreateInstance<T>(serviceProvider, parameters);
        }
    }
}
