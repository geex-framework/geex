using Geex.Extensions.ApprovalFlows;
using Geex.Gql;
using Geex.Gql.Types;
using Geex.Tests.FeatureTests;
using Geex.Tests.SchemaTests.TestEntities;
using HotChocolate;
using HotChocolate.Types;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Geex.Tests.SchemaTests
{
    public class ApproveEntityMutation : MutationExtension<ApproveEntityMutation>, IHasApproveMutation<ApproveEntity>
    {
        /// <inheritdoc />
        protected override void Configure(IObjectTypeDescriptor<ApproveEntityMutation> descriptor)
        {
            base.Configure(descriptor);
        }
        public ApproveEntity ApproveEntityTestMutationField(string arg1) => throw new NotImplementedException();

    }
    [Collection(nameof(TestsCollection))]
    public class ApproveEntityTypeInterceptorTests : TestsBase
    {
        public ApproveEntityTypeInterceptorTests(TestApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task ApproveEntityFieldsShouldBeAdded()
        {
            // Arrange
            
            var schema = ScopedService.GetService<ISchema>();

            // Act & Assert
            // Check if ApproveStatus field is configured for IApproveEntity types
            var approveEntityType = schema.GetType<ObjectType>(nameof(ApproveEntity));
            approveEntityType.ShouldNotBeNull();
            approveEntityType.Fields.TryGetField("approveStatus", out var approveStatusField).ShouldBeTrue();
            approveEntityType.Fields.TryGetField("submittable", out var submittableField).ShouldBeTrue();
        }

        [Fact]
        public async Task ApproveMutationMethodsShouldBeAdded()
        {
            // Arrange
            
            var schema = ScopedService.GetService<ISchema>();

            // Act & Assert
            // Check if the mutation type has the approve methods added
            var approveEntityName = nameof(ApproveEntity);
            schema.MutationType.Fields.TryGetField($"submit{approveEntityName}", out var submitField).ShouldBeTrue();
            schema.MutationType.Fields.TryGetField($"approve{approveEntityName}", out var approveField).ShouldBeTrue();
            schema.MutationType.Fields.TryGetField($"unSubmit{approveEntityName}", out var unSubmitField).ShouldBeTrue();
            schema.MutationType.Fields.TryGetField($"unApprove{approveEntityName}", out var unApproveField).ShouldBeTrue();

            schema.GetType<ObjectType>(nameof(ApproveEntity)).Fields.TryGetField("approveStatus", out var approveStatusField).ShouldBeTrue();
            approveStatusField.Type.RuntimeType.ShouldBe(typeof(ApproveStatus));
            schema.GetType<ObjectType>(nameof(ApproveEntity)).Fields.TryGetField("submittable", out var submittableField).ShouldBeTrue();
            submittableField.Type.RuntimeType.ShouldBe(typeof(bool));
            // Verify field arguments
            submitField.Arguments.Count.ShouldBe(2);
            submitField.Arguments.Any(a => a.Name == "ids").ShouldBeTrue();
            submitField.Arguments.Any(a => a.Name == "remark").ShouldBeTrue();

            approveField.Arguments.Count.ShouldBe(2);
            approveField.Arguments.Any(a => a.Name == "ids").ShouldBeTrue();
            approveField.Arguments.Any(a => a.Name == "remark").ShouldBeTrue();

            unSubmitField.Arguments.Count.ShouldBe(2);
            unSubmitField.Arguments.Any(a => a.Name == "ids").ShouldBeTrue();
            unSubmitField.Arguments.Any(a => a.Name == "remark").ShouldBeTrue();

            unApproveField.Arguments.Count.ShouldBe(2);
            unApproveField.Arguments.Any(a => a.Name == "ids").ShouldBeTrue();
            unApproveField.Arguments.Any(a => a.Name == "remark").ShouldBeTrue();
        }

        [Fact]
        public async Task AuditDirectiveShouldBeAddedToApproveMethods()
        {
            // Arrange
            
            var schema = ScopedService.GetService<ISchema>();

            // Act & Assert
            // Check if GeexTypeInterceptor.AuditTypes applies audit directive to methods
            var approveEntityName = nameof(ApproveEntity);

            schema.MutationType.Fields.TryGetField($"submit{approveEntityName}", out var submitField).ShouldBeTrue();
            schema.MutationType.Fields.TryGetField($"approve{approveEntityName}", out var approveField).ShouldBeTrue();

            // Verify audit directives are added when the type is in GeexTypeInterceptor.AuditTypes
            if (GeexTypeInterceptor.AuditTypes.Contains(typeof(ApproveEntityMutation)))
            {
                submitField.Directives.Any(x => x.Type.Name == "audit").ShouldBeTrue();
                approveField.Directives.Any(x => x.Type.Name == "audit").ShouldBeTrue();
            }
        }
    }
}
