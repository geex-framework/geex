using System.Linq;

using Geex.Gql.AutoBatchLoad;
using Geex.Gql.Types;
using Geex.Tests.FeatureTests.TestEntities;

using HotChocolate.Types;

namespace Geex.Tests.FeatureTests
{
    public class CoreBatchLoadQuery : QueryExtension<CoreBatchLoadQuery>
    {
        private readonly IUnitOfWork _uow;

        public CoreBatchLoadQuery(IUnitOfWork uow)
        {
            _uow = uow;
        }

        protected override void Configure(IObjectTypeDescriptor<CoreBatchLoadQuery> descriptor)
        {
            descriptor.Field(x => x.CoreBatchLoadPaged(default))
                .UseOffsetPaging<ObjectType<BatchLoadTestEntity>>();

            descriptor.Field(x => x.CoreBatchLoadOptOut(default))
                .UseAutoBatchLoad(false);

            base.Configure(descriptor);
        }

        public IQueryable<BatchLoadTestEntity> CoreBatchLoadPaged(string? thisId) =>
            _uow.Query<BatchLoadTestEntity>()
                .WhereIf(!string.IsNullOrEmpty(thisId), x => x.ThisId == thisId);

        public IQueryable<BatchLoadTestEntity> CoreBatchLoadList() =>
            _uow.Query<BatchLoadTestEntity>();

        public BatchLoadTestEntity? CoreBatchLoadById(string thisId) =>
            _uow.Query<BatchLoadTestEntity>().FirstOrDefault(x => x.ThisId == thisId);

        public BatchLoadTestEntity? CoreBatchLoadOptOut(string thisId) =>
            _uow.Query<BatchLoadTestEntity>().FirstOrDefault(x => x.ThisId == thisId);
    }
}
