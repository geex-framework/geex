using Geex.Extensions.AuditLogs;
using Geex.Extensions.AuditLogs.Utils;
using Geex.Gql.Types;
using HotChocolate;
using HotChocolate.Types;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Geex.Tests.SchemaTests
{
    public class AuditLogTestMutation : MutationExtension<AuditLogTestMutation>
    {
        /// <inheritdoc />
        protected override void Configure(IObjectTypeDescriptor<AuditLogTestMutation> descriptor)
        {
            descriptor.AuditFieldsImplicitly();
            base.Configure(descriptor);
        }

        public bool AuditLogTestMutationField(string arg1) => throw new NotImplementedException();
    }
    [Collection(nameof(TestsCollection))]
    public class AuditLogsTypeInterceptorTests : TestsBase
    {
        public AuditLogsTypeInterceptorTests(TestApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task AuditFieldsImplicitlyShouldWork()
        {
            // Arrange
            
            var schema = ScopedService.GetService<ISchema>();
            schema.MutationType.Fields.TryGetField(nameof(AuditLogTestMutation.AuditLogTestMutationField).ToCamelCase(), out var field1).ShouldBeTrue();
            field1.Directives.Any(x => x.Type.RuntimeType == typeof(AuditDirectiveType)).ShouldBeTrue();
        }

        [Fact]
        public async Task AuthenticationMutationShouldBePatched()
        {
            // Arrange
            
            var schema = ScopedService.GetService<ISchema>();
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
