using System.Linq;

using Geex.Gql.AutoBatchLoad;
using Geex.Gql.Types;
using Geex.Tests.SchemaTests.TestEntities;

using HotChocolate.Types;

namespace Geex.Tests.SchemaTests
{
    public class AutoBatchLoadTestQuery : QueryExtension<AutoBatchLoadTestQuery>
    {
        private readonly IUnitOfWork _uow;

        public AutoBatchLoadTestQuery(IUnitOfWork uow)
        {
            _uow = uow;
        }

        protected override void Configure(IObjectTypeDescriptor<AutoBatchLoadTestQuery> descriptor)
        {
            descriptor.Field(x => x.AutoBatchLoadPaged(default))
                .UseOffsetPaging<ObjectType<AutoBatchLoadTestEntity>>();

            descriptor.Field(x => x.AutoBatchLoadOptOut(default))
                .UseAutoBatchLoad(false);

            base.Configure(descriptor);
        }

        public IQueryable<AutoBatchLoadTestEntity> AutoBatchLoadPaged(string? thisId) =>
            _uow.Query<AutoBatchLoadTestEntity>()
                .WhereIf(!string.IsNullOrEmpty(thisId), x => x.ThisId == thisId);

        public IQueryable<AutoBatchLoadTestEntity> AutoBatchLoadList() =>
            _uow.Query<AutoBatchLoadTestEntity>();

        public AutoBatchLoadTestEntity? AutoBatchLoadById(string thisId) =>
            _uow.Query<AutoBatchLoadTestEntity>().FirstOrDefault(x => x.ThisId == thisId);

        public AutoBatchLoadTestEntity? AutoBatchLoadOptOut(string thisId) =>
            _uow.Query<AutoBatchLoadTestEntity>().FirstOrDefault(x => x.ThisId == thisId);

        public IQueryable<AutoBatchLoadTestEntity> AutoBatchLoadManualChildren() =>
            _uow.Query<AutoBatchLoadTestEntity>().BatchLoad(x => x.Children);

        public IQueryable<AutoBatchLoadTestEntity> AutoBatchLoadNonCachedList() =>
            _uow.Query<AutoBatchLoadTestEntity>().ToList().AsQueryable();
    }
}
