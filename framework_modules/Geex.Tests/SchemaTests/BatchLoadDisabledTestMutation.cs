using System.Linq;

using Geex.Gql.AutoBatchLoad;
using Geex.Gql.Types;
using Geex.Tests.SchemaTests.TestEntities;

using HotChocolate.Types;

namespace Geex.Tests.SchemaTests
{
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

        public IQueryable<BatchLoadGraphQLEntity> BatchLoadEntitiesDisabled() =>
            _uow.Query<BatchLoadGraphQLEntity>()
                .Where(x => string.IsNullOrEmpty(x.ParentId));
    }
}
