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
                .UseOffsetPaging<ObjectType<BatchLoadGraphQLEntity>>();
            descriptor.Field(x => x.BatchLoadInterfaceEntitiesPaged(default))
                .UseAutoBatchLoad(true)
                .UseOffsetPaging<InterfaceType<IBatchLoadGraphQLEntity>>();

            base.Configure(descriptor);
        }

        public IQueryable<BatchLoadGraphQLEntity> BatchLoadEntities() =>
            RootEntities();

        public IQueryable<IBatchLoadGraphQLEntity> BatchLoadInterfaceEntities() =>
            RootEntities();

        public IQueryable<BatchLoadGraphQLEntity> BatchLoadEntitiesPaged(string? thisId) =>
            RootEntities()
                .WhereIf(!string.IsNullOrEmpty(thisId), x => x.ThisId == thisId);

        public IQueryable<IBatchLoadGraphQLEntity> BatchLoadInterfaceEntitiesPaged(string? thisId) =>
            RootEntities()
                .WhereIf(!string.IsNullOrEmpty(thisId), x => x.ThisId == thisId);

        public IQueryable<BatchLoadGraphQLEntity> BatchLoadEntitiesFiltered(string? thisId) =>
            RootEntities()
                .WhereIf(!string.IsNullOrEmpty(thisId), x => x.ThisId == thisId);

        public IQueryable<BatchLoadGraphQLEntity> BatchLoadEntitiesManualOrphan() =>
            RootEntities()
                .BatchLoad(x => x.Children)
                .ThenBatchLoad(x => x.FirstChild);

        public IQueryable<BatchLoadGraphQLEntity> BatchLoadEntitiesManualPartial() =>
            RootEntities()
                .BatchLoad(x => x.Children);

        public IQueryable<BatchLoadGraphQLEntity> BatchLoadEntitiesManualOnly() =>
            RootEntities()
                .BatchLoad(x => x.FirstChild);

        private IQueryable<BatchLoadGraphQLEntity> RootEntities() =>
            _uow.Query<BatchLoadGraphQLEntity>()
                .Where(x => string.IsNullOrEmpty(x.ParentId));
    }
}
