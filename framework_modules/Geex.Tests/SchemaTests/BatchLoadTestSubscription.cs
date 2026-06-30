using System;
using System.Linq;

using Geex.Gql.AutoBatchLoad;
using Geex.Gql.Types;
using Geex.Tests.SchemaTests.TestEntities;

using HotChocolate;
using HotChocolate.Types;

namespace Geex.Tests.SchemaTests
{
    public class BatchLoadTestSubscription : SubscriptionExtension<BatchLoadTestSubscription>
    {
        private readonly IUnitOfWork _uow;

        public BatchLoadTestSubscription(IUnitOfWork uow)
        {
            _uow = uow;
        }

        protected override void Configure(IObjectTypeDescriptor<BatchLoadTestSubscription> descriptor)
        {
            descriptor.UseAutoBatchLoad(true);
            base.Configure(descriptor);
        }

        [SubscribeAndResolve]
        public IObservable<IQueryable<BatchLoadGraphQLEntity>> OnBatchLoadEntities()
        {
            var queryable = _uow.Query<BatchLoadGraphQLEntity>()
                .Where(x => string.IsNullOrEmpty(x.ParentId));
            return new ImmediateObservable<IQueryable<BatchLoadGraphQLEntity>>(queryable);
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
