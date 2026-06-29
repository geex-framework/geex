using System.Linq;

using Geex.Gql.AutoBatchLoad;
using Geex.Gql.Types;
using Geex.Tests.SchemaTests.TestEntities;

using HotChocolate.Data;
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
            descriptor.Field(x => x.BatchLoadEntitiesPaged(default))
                .UseOffsetPaging<ObjectType<BatchLoadGraphQLEntity>>();

            descriptor.Field(x => x.BatchLoadEntitiesFiltered())
                .UseFiltering();

            base.Configure(descriptor);
        }

        public IQueryable<BatchLoadGraphQLEntity> BatchLoadEntities() =>
            _uow.Query<BatchLoadGraphQLEntity>();

        public IQueryable<BatchLoadGraphQLEntity> BatchLoadEntitiesPaged(string? thisId) =>
            _uow.Query<BatchLoadGraphQLEntity>()
                .WhereIf(!string.IsNullOrEmpty(thisId), x => x.ThisId == thisId);

        public IQueryable<BatchLoadGraphQLEntity> BatchLoadEntitiesFiltered() =>
            _uow.Query<BatchLoadGraphQLEntity>();

        public IQueryable<BatchLoadGraphQLEntity> BatchLoadEntitiesManualOrphan() =>
            _uow.Query<BatchLoadGraphQLEntity>()
                .BatchLoad(x => x.Children)
                .ThenBatchLoad(x => x.FirstChild);

        public IQueryable<BatchLoadGraphQLEntity> BatchLoadEntitiesManualPartial() =>
            _uow.Query<BatchLoadGraphQLEntity>()
                .BatchLoad(x => x.Children);
    }
}
