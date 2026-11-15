using Geex.Extensions.AuditLogs;
using Geex.Extensions.AuditLogs.Utils;
using Geex.Gql.Types;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Geex.Tests.SchemaTests;

public class AuditPolicySchemaTests
{
    [Theory]
    [InlineData(AuditLevel.None, false, false, false)]
    [InlineData(AuditLevel.MutationOnly, false, true, false)]
    [InlineData(AuditLevel.All, true, true, true)]
    public async Task DefaultLevelSelectsRootOperations(AuditLevel level, bool query, bool mutation, bool subscription)
    {
        var schema = await CreateSchema(level);
        AssertAudit(schema.QueryType, "unconfiguredQuery", query);
        AssertAudit(schema.MutationType!, "unconfiguredMutation", mutation);
        AssertAudit(schema.SubscriptionType!, "changes", subscription);
        AssertAudit(schema.MutationType!, "authenticate", true);
        AssertAudit(schema.MutationType!, "federateAuthenticate", false);
        AssertAudit(schema.MutationType!, "cancelAuthentication", true);
    }

    [Theory]
    [InlineData(AuditLevel.None, AuditLevel.All, false)]
    [InlineData(AuditLevel.None, AuditLevel.All, true)]
    [InlineData(AuditLevel.All, AuditLevel.None, false)]
    [InlineData(AuditLevel.All, AuditLevel.None, true)]
    [InlineData(AuditLevel.All, AuditLevel.MutationOnly, false)]
    [InlineData(AuditLevel.All, AuditLevel.MutationOnly, true)]
    public async Task FieldOverridesTypeAndTypeOverridesDefaultRegardlessOfDeclarationOrder(
        AuditLevel globalLevel, AuditLevel typeLevel, bool fieldsFirst)
    {
        var schema = await CreateSchema(globalLevel, typeLevel, fieldsFirst);
        AssertAudit(schema.QueryType, "typeQuery", typeLevel == AuditLevel.All);
        AssertAudit(schema.MutationType!, "typeMutation", typeLevel != AuditLevel.None);
        AssertAudit(schema.QueryType, "enabledQuery", true);
        AssertAudit(schema.QueryType, "disabledQuery", false);
        AssertAudit(schema.MutationType!, "enabledMutation", true);
        AssertAudit(schema.MutationType!, "disabledMutation", false);
    }

    [Fact]
    public async Task SeparateSchemasDoNotShareTypeOrDefaultPolicies()
    {
        var first = await CreateSchema(AuditLevel.None, AuditLevel.All);
        var second = await CreateSchema(AuditLevel.All, AuditLevel.None);
        var third = await CreateSchema();
        new AuditLogsModuleOptions().DefaultAuditLevel.ShouldBe(AuditLevel.None);
        AssertAudit(first.QueryType, "typeQuery", true);
        AssertAudit(second.QueryType, "typeQuery", false);
        AssertAudit(third.QueryType, "typeQuery", false);
        AssertAudit(third.MutationType!, "unconfiguredMutation", false);
    }

    private static void AssertAudit(IObjectType type, string fieldName, bool enabled)
    {
        type.Fields[fieldName].Directives.Count(x => x.Type.Name == AuditDirectiveType.DirectiveName)
            .ShouldBe(enabled ? 1 : 0);
    }

    private static async Task<ISchema> CreateSchema(AuditLevel? globalLevel = null, AuditLevel? typeLevel = null,
        bool fieldsFirst = false)
    {
        var options = new AuditLogsModuleOptions();
        if (globalLevel.HasValue) options.DefaultAuditLevel = globalLevel.Value;
        var builder = new ServiceCollection().AddGraphQLServer()
            .AddQueryType(descriptor =>
            {
                descriptor.Name("Query");
                descriptor.Field("unconfiguredQuery").Type<BooleanType>().Resolve(true);
            })
            .AddMutationType(descriptor =>
            {
                descriptor.Name("Mutation");
                descriptor.Field("unconfiguredMutation").Type<BooleanType>().Resolve(true);
                descriptor.Field("authenticate").Type<BooleanType>().Resolve(true);
                descriptor.Field("federateAuthenticate").Type<BooleanType>().Resolve(true).Audit(false);
                descriptor.Field("cancelAuthentication").Type<BooleanType>().Resolve(true).Audit().Audit();
            })
            .AddSubscriptionType<PolicySubscription>(descriptor => descriptor.Name("Subscription"))
            .AddDirectiveType<AuditDirectiveType.Config>()
            .ConfigureSchema(schema =>
            {
                schema.AddType(new PolicyQuery(typeLevel, fieldsFirst));
                schema.AddType(new PolicyMutation(typeLevel, fieldsFirst));
                schema.TryAddTypeInterceptor(new AuditLogsTypeInterceptor(options));
            });
        return await builder.BuildSchemaAsync();
    }

    private sealed class PolicyQuery(AuditLevel? level, bool fieldsFirst) : QueryExtension<PolicyQuery>
    {
        protected override void Configure(IObjectTypeDescriptor<PolicyQuery> descriptor)
        {
            descriptor.BindFieldsExplicitly();
            if (!fieldsFirst && level.HasValue) descriptor.AuditFieldsImplicitly(level.Value);
            descriptor.Field("typeQuery").Type<BooleanType>().Resolve(true);
            descriptor.Field("enabledQuery").Type<BooleanType>().Resolve(true).Audit();
            descriptor.Field("disabledQuery").Type<BooleanType>().Resolve(true).Audit(false);
            if (fieldsFirst && level.HasValue) descriptor.AuditFieldsImplicitly(level.Value);
            base.Configure(descriptor);
        }
    }

    private sealed class PolicyMutation(AuditLevel? level, bool fieldsFirst) : MutationExtension<PolicyMutation>
    {
        protected override void Configure(IObjectTypeDescriptor<PolicyMutation> descriptor)
        {
            descriptor.BindFieldsExplicitly();
            if (!fieldsFirst && level.HasValue) descriptor.AuditFieldsImplicitly(level.Value);
            descriptor.Field("typeMutation").Type<BooleanType>().Resolve(true);
            descriptor.Field("enabledMutation").Type<BooleanType>().Resolve(true).Audit();
            descriptor.Field("disabledMutation").Type<BooleanType>().Resolve(true).Audit(false);
            if (fieldsFirst && level.HasValue) descriptor.AuditFieldsImplicitly(level.Value);
            base.Configure(descriptor);
        }
    }

    private sealed class PolicySubscription
    {
        [SubscribeAndResolve]
        public IObservable<bool> Changes() => new ImmediateObservable<bool>(true);
    }
}
