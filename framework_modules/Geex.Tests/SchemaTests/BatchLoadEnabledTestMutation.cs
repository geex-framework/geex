using System.Linq;

using Geex.Gql.AutoBatchLoad;
using Geex.Gql.Types;
using Geex.Tests.SchemaTests.TestEntities;

using HotChocolate.Types;

namespace Geex.Tests.SchemaTests
{
    public class BatchLoadEnabledTestMutation : MutationExtension<BatchLoadEnabledTestMutation>
    {
        private readonly IUnitOfWork _uow;

        public BatchLoadEnabledTestMutation(IUnitOfWork uow)
        {
            _uow = uow;
        }

        protected override void Configure(IObjectTypeDescriptor<BatchLoadEnabledTestMutation> descriptor)
        {
            descriptor.UseAutoBatchLoad(true);
            base.Configure(descriptor);
        }

        public IQueryable<BatchLoadGraphQLEntity> BatchLoadEntitiesEnabled() =>
            _uow.Query<BatchLoadGraphQLEntity>()
                .Where(x => string.IsNullOrEmpty(x.ParentId));
    }
}
