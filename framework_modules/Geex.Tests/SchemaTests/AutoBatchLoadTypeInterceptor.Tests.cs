using System;
using System.Linq;

using Geex.Gql.AutoBatchLoad;
using Geex.Gql.Types;
using Geex.Tests.SchemaTests.TestEntities;

using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;

namespace Geex.Tests.SchemaTests
{
    public class BatchLoadTestQuery : QueryExtension<BatchLoadTestQuery>
    {
        private readonly IUnitOfWork _uow;

        public BatchLoadTestQuery(IUnitOfWork uow)
        {
            _uow = uow;
        }

        protected override void Configure(IObjectTypeDescriptor<BatchLoadTestQuery> descriptor)
        {
            descriptor.Field(x => x.BatchLoadEntities()).UseAutoBatchLoad(true);
            descriptor.Field(x => x.BatchLoadInterfaceEntities()).UseAutoBatchLoad(true);
            descriptor.Field(x => x.BatchLoadEntitiesManualOrphan()).UseAutoBatchLoad(true);
            descriptor.Field(x => x.BatchLoadEntitiesManualPartial()).UseAutoBatchLoad(true);
            descriptor.Field(x => x.BatchLoadEntitiesManualOnly()).UseAutoBatchLoad(true);
            descriptor.Field(x => x.BatchLoadEntitiesFiltered(default)).UseAutoBatchLoad(true);
            descriptor.Field(x => x.BatchLoadEntitiesPaged(default))
                .UseAutoBatchLoad(true)
                .UseOffsetPaging<ObjectType<BatchLoadTestEntity>>();
            descriptor.Field(x => x.BatchLoadInterfaceEntitiesPaged(default))
                .UseAutoBatchLoad(true)
                .UseOffsetPaging<InterfaceType<IBatchLoadTestEntity>>();

            base.Configure(descriptor);
        }

        public IQueryable<BatchLoadTestEntity> BatchLoadEntities() =>
            RootEntities();

        public IQueryable<IBatchLoadTestEntity> BatchLoadInterfaceEntities() =>
            RootEntities();

        public IQueryable<BatchLoadTestEntity> BatchLoadEntitiesPaged(string? thisId) =>
            RootEntities()
                .WhereIf(!string.IsNullOrEmpty(thisId), x => x.ThisId == thisId);

        public IQueryable<IBatchLoadTestEntity> BatchLoadInterfaceEntitiesPaged(string? thisId) =>
            RootEntities()
                .WhereIf(!string.IsNullOrEmpty(thisId), x => x.ThisId == thisId);

        public IQueryable<BatchLoadTestEntity> BatchLoadEntitiesFiltered(string? thisId) =>
            RootEntities()
                .WhereIf(!string.IsNullOrEmpty(thisId), x => x.ThisId == thisId);

        public IQueryable<BatchLoadTestEntity> BatchLoadEntitiesManualOrphan() =>
            RootEntities()
                .BatchLoad(x => x.Children)
                .ThenBatchLoad(x => x.FirstChild);

        public IQueryable<BatchLoadTestEntity> BatchLoadEntitiesManualPartial() =>
            RootEntities()
                .BatchLoad(x => x.Children);

        public IQueryable<BatchLoadTestEntity> BatchLoadEntitiesManualOnly() =>
            RootEntities()
                .BatchLoad(x => x.FirstChild);

        private IQueryable<BatchLoadTestEntity> RootEntities() =>
            _uow.Query<BatchLoadTestEntity>()
                .Where(x => string.IsNullOrEmpty(x.ParentId));
    }

    public class BatchLoadEnabledTestMutation : MutationExtension<BatchLoadEnabledTestMutation>
    {
        private readonly IUnitOfWork _uow;

        public BatchLoadEnabledTestMutation(IUnitOfWork uow)
        {
            _uow = uow;
        }

        protected override void Configure(IObjectTypeDescriptor<BatchLoadEnabledTestMutation> descriptor)
        {
            descriptor.Field(x => x.BatchLoadEntitiesEnabled()).UseAutoBatchLoad(true);
            base.Configure(descriptor);
        }

        public IQueryable<BatchLoadTestEntity> BatchLoadEntitiesEnabled() =>
            _uow.Query<BatchLoadTestEntity>()
                .Where(x => string.IsNullOrEmpty(x.ParentId));
    }

    public class BatchLoadDisabledTestMutation : MutationExtension<BatchLoadDisabledTestMutation>
    {
        private readonly IUnitOfWork _uow;

        public BatchLoadDisabledTestMutation(IUnitOfWork uow)
        {
            _uow = uow;
        }

        protected override void Configure(IObjectTypeDescriptor<BatchLoadDisabledTestMutation> descriptor)
        {
            descriptor.UseAutoBatchLoad(false);
            base.Configure(descriptor);
        }

        public IQueryable<BatchLoadTestEntity> BatchLoadEntitiesDisabled() =>
            _uow.Query<BatchLoadTestEntity>()
                .Where(x => string.IsNullOrEmpty(x.ParentId));
    }

    public class BatchLoadTestSubscription : SubscriptionExtension<BatchLoadTestSubscription>
    {
        private readonly IUnitOfWork _uow;

        public BatchLoadTestSubscription(IUnitOfWork uow)
        {
            _uow = uow;
        }

        protected override void Configure(IObjectTypeDescriptor<BatchLoadTestSubscription> descriptor)
        {
            descriptor.Field(x => x.OnBatchLoadEntities()).UseAutoBatchLoad(true);
            base.Configure(descriptor);
        }

        [SubscribeAndResolve]
        public IObservable<IQueryable<BatchLoadTestEntity>> OnBatchLoadEntities()
        {
            var queryable = _uow.Query<BatchLoadTestEntity>()
                .Where(x => string.IsNullOrEmpty(x.ParentId));
            return new ImmediateObservable<IQueryable<BatchLoadTestEntity>>(queryable);
        }
    }

    internal sealed class ImmediateObservable<T> : IObservable<T>
    {
        private readonly T _value;

        public ImmediateObservable(T value) => _value = value;

        public IDisposable Subscribe(IObserver<T> observer)
        {
            observer.OnNext(_value);
            observer.OnCompleted();
            return EmptyDisposable.Instance;
        }
    }

    internal sealed class EmptyDisposable : IDisposable
    {
        public static readonly EmptyDisposable Instance = new();

        public void Dispose()
        {
        }
    }
}
