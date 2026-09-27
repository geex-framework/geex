using Geex.Extensions.BlobStorage;
using Geex.Extensions.BlobStorage.Core.Entities;
using Geex.Extensions.Backups.Core.Entities;
using Geex.Extensions.Backups.Gql;
using Geex.Gql;
using Geex.Gql.Types;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Validation;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Pagination;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace Geex.Extensions.Backups.Tests;

public class BackupSchemaTests
{
    [Fact]
    public async Task SchemaExposesBackupsOperationsPermissionsAndFrontendContract()
    {
        var services = new ServiceCollection();
        var builder = services.AddGraphQLServer()
            .AddQueryType<Query>()
            .AddMutationType<Mutation>()
            .AddSubscriptionType(descriptor => descriptor.Name("Subscription"))
            .AddTypeExtension<BackupSubscription>()
            .AddTypeExtension<BackupQuery>()
            .AddTypeExtension<BackupMutation>()
            .AddType<BackupType>()
            .AddAuthorization()
            .AddFiltering<GeexFilterConvention>()
            .AddConvention<INamingConventions>(new GeexNamingConventions(new XmlDocumentationProvider(
                new XmlDocumentationFileResolver(), new DefaultObjectPool<StringBuilder>(new StringBuilderPooledObjectPolicy()))))
            .SetPagingOptions(new PagingOptions { IncludeTotalCount = true })
            .AddType(new ObjectType<BlobObject>(descriptor =>
            {
                descriptor.BindFieldsExplicitly();
                descriptor.Implements<InterfaceType<IBlobObject>>();
                descriptor.Field(x => x.Id);
                descriptor.Field(x => x.FileName);
                descriptor.Field(x => x.FileSize);
                descriptor.Field(x => x.Url);
            }))
            .AddType(new InterfaceType<IBlobObject>(descriptor =>
            {
                descriptor.BindFieldsExplicitly();
                descriptor.Field(x => x.Id);
                descriptor.Field(x => x.FileName);
                descriptor.Field(x => x.FileSize);
                descriptor.Field(x => x.Url);
            }));
        var executor = await builder.BuildRequestExecutorAsync();
        var schema = executor.Schema;
        var mutation = schema.MutationType!;
        Assert.IsType<BooleanType>(Assert.IsType<NonNullType>(mutation.Fields["startBackup"].Type).Type);
        Assert.IsType<StringType>(mutation.Fields["startBackupTracked"].Type);
        Assert.IsType<BooleanType>(Assert.IsType<NonNullType>(mutation.Fields["expireBackup"].Type).Type);
        Assert.IsType<StringType>(Assert.IsType<NonNullType>(mutation.Fields["expireBackup"].Arguments["id"].Type).Type);
        var definition = Utf8GraphQLParser.Parse(schema.ToString()).Definitions.OfType<ObjectTypeDefinitionNode>()
            .Single(x => x.Name.Value == "Mutation");
        foreach (var field in new[] { "startBackup", "startBackupTracked", "expireBackup" })
        {
            Assert.Single(mutation.Fields[field].Directives, directive => directive.Type.Name == "authorize");
            var policy = definition.Fields.Single(x => x.Name.Value == field).Directives.Single(x => x.Name.Value == "authorize")
                .Arguments.Single(x => x.Name.Value == "policy").Value;
            Assert.Equal(field == "expireBackup" ? BackupsPermission.Expire.Value : BackupsPermission.Create.Value,
                Assert.IsType<StringValueNode>(policy).Value);
        }
        Assert.Contains(schema.QueryType.Fields["backups"].Directives, directive => directive.Type.Name == "authorize");
        var query = schema.QueryType.Fields["backups"];
        Assert.Contains(query.Arguments, argument => argument.Name == "filter");
        Assert.Contains(query.Arguments, argument => argument.Name == "skip");
        Assert.Contains(query.Arguments, argument => argument.Name == "take");
        Assert.Equal(new[] { "Running", "Succeeded", "Failed", "Cancelled", "Expired" },
            schema.GetType<EnumType>(nameof(BackupStatus)).Values.Select(value => value.Name));
        Assert.Equal(new[] { "Unknown", "Automatic", "Manual" },
            schema.GetType<EnumType>(nameof(BackupSource)).Values.Select(value => value.Name));

        var source = await File.ReadAllTextAsync(System.IO.Path.Combine(AppContext.BaseDirectory, "backups.module.ts"));
        var operations = Regex.Matches(source, @"gql`([^`]+)`");
        Assert.Equal(5, operations.Count);
        var validationRegistration = new ServiceCollection();
        validationRegistration.AddValidation(schema.Name)
            .AddDocumentRules().AddOperationRules().AddFieldRules().AddFragmentRules()
            .AddValueRules().AddArgumentRules().AddDirectiveRules().AddVariableRules();
        using var validationServices = validationRegistration.BuildServiceProvider();
        var validator = validationServices.GetRequiredService<IDocumentValidatorFactory>().CreateValidator(schema.Name);
        foreach (Match operation in operations)
        {
            var result = await validator.ValidateAsync(schema, Utf8GraphQLParser.Parse(operation.Groups[1].Value),
                Guid.NewGuid().ToString(), new Dictionary<string, object?>(), false, CancellationToken.None);
            Assert.Empty(result.Errors);
        }
        var invalid = await validator.ValidateAsync(schema, Utf8GraphQLParser.Parse("{ backups { unknownField } }"),
            Guid.NewGuid().ToString(), new Dictionary<string, object?>(), false, CancellationToken.None);
        Assert.NotEmpty(invalid.Errors);
    }
}
