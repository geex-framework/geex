using Geex.ApprovalFlows;
using Geex.Extensions.AuditLogs;
using Geex.Gql.Types;
using Geex.Tests.TestEntities;

using HotChocolate;
using HotChocolate.Types;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Geex.Tests.SchemaTests
{
    public class AuditLogsTypeInterceptorTests : IClassFixture<GeexWebApplicationFactory>
    {
        public class AuditLogMutation : MutationExtension<AuditLogMutation>, IHasApproveMutation<ApproveEntity>
        {
            /// <inheritdoc />
            protected override void Configure(IObjectTypeDescriptor<AuditLogMutation> descriptor)
            {
                descriptor.AuditFieldsImplicitly();
                base.Configure(descriptor);
            }

            public bool AuditLogMutationField1(string arg1) => throw new NotImplementedException();
        }

        private readonly GeexWebApplicationFactory _factory;

        public AuditLogsTypeInterceptorTests(GeexWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task AuditFieldsImplicitlyShouldWork()
        {
            // Arrange
            var service = _factory.Services;
            var schema = service.GetService<ISchema>();
            schema.MutationType.Fields.TryGetField("auditLogMutationField1", out var field1).ShouldBeTrue();
            field1.Directives.Any(x => x.Type.RuntimeType == typeof(AuditDirectiveType)).ShouldBeTrue();
        }

        [Fact]
        public async Task AuthenticationMutationShouldBePatched()
        {
            // Arrange
            var service = _factory.Services;
            var schema = service.GetService<ISchema>();
            var toBePatchFieldNames = AuditLogsTypeInterceptor.ToBePatchedBuiltInOperations.SelectMany(x => x.Value);
            foreach (var fieldName in toBePatchFieldNames)
            {
                var field1 = schema.MutationType.Fields.FirstOrDefault(x => x.Name.Equals(fieldName, StringComparison.InvariantCultureIgnoreCase));
                var match = field1.Directives.FirstOrDefault(x => x.Type.Name == AuditDirectiveType.DirectiveName);
                match.ShouldNotBeNull();
            }
        }
    }
}
