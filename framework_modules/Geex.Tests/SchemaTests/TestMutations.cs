using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Geex.ApprovalFlows;
using Geex.Extensions.AuditLogs;
using Geex.Gql.Types;
using Geex.Tests.TestEntities;

using HotChocolate.Types;

namespace Geex.Tests.SchemaTests
{
    public class ApproveMutation : MutationExtension<ApproveMutation>, IHasApproveMutation<ApproveEntity>
    {
        /// <inheritdoc />
        protected override void Configure(IObjectTypeDescriptor<ApproveMutation> descriptor)
        {
            base.Configure(descriptor);
        }
        public ApproveEntity TestMutation(string arg1) => throw new NotImplementedException();

    }

    public class AuditLogTestMutation : MutationExtension<AuditLogTestMutation>
    {
        /// <inheritdoc />
        protected override void Configure(IObjectTypeDescriptor<AuditLogTestMutation> descriptor)
        {
            descriptor.AuditFieldsImplicitly();
            base.Configure(descriptor);
        }

        public bool AuditLogMutationField1(string arg1) => throw new NotImplementedException();
    }

    public class CoreTestMutation : MutationExtension<CoreTestMutation>
    {
        /// <inheritdoc />
        protected override void Configure(IObjectTypeDescriptor<CoreTestMutation> descriptor)
        {
            base.Configure(descriptor);
        }

        public TestEntity DirectMutation(string arg1) => throw new NotImplementedException();
        public Lazy<TestEntity> LazyMutation(string arg1) => throw new NotImplementedException();
        public IQueryable<TestEntity> IQueryableMutation(string arg1) => throw new NotImplementedException();
    }
}
