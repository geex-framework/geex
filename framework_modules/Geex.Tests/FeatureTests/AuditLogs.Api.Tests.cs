using Geex.Extensions.AuditLogs;
using Geex.Extensions.AuditLogs.Core.Entities;
using Geex.Extensions.AuditLogs.Requests;
using Geex.Gql.Types;
using HotChocolate;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using Shouldly;

namespace Geex.Tests.FeatureTests;

public class AuditWriteTestMutation : MutationExtension<AuditWriteTestMutation>
{
    protected override void Configure(IObjectTypeDescriptor<AuditWriteTestMutation> descriptor)
    {
        descriptor.Field(x => x.AuditWriteSuccess(default!)).Audit();
        descriptor.Field(x => x.AuditWriteDisabled(default!)).Audit(false);
        descriptor.Field(x => x.AuditWriteFailure(default!)).Audit();
        descriptor.Field(x => x.AuditWriteReportedFailure(default!, default!)).Audit();
        base.Configure(descriptor);
    }

    public string? AuditWriteSuccess(string marker) => marker;
    public string? AuditWriteDisabled(string marker) => marker;
    public string? AuditWriteFailure(string marker) => throw new InvalidOperationException(marker);
    public string? AuditWriteReportedFailure(string marker, IResolverContext context)
    {
        context.ReportError(ErrorBuilder.New().SetMessage(marker).SetCode("AUDIT_TEST_REPORTED_ERROR").Build());
        return null;
    }
}

[Collection(nameof(TestsCollection))]
public class AuditLogsApiTests : TestsBase
{
    public AuditLogsApiTests(TestApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task QueryAuditLogsShouldWork()
    {
        var logId = ObjectId.GenerateNewId().ToString();
        using (var scope = ScopedService.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            uow.Attach(new AuditLog
            {
                OperationName = "testOperation",
                OperationType = OperationType.Mutation,
                IsSuccess = true,
                OperatorId = GeexConstants.SuperAdminId,
            });
            await uow.SaveChanges();
        }

        var client = SuperAdminClient;
        var query = """
            query {
                auditLogs(take: 10) {
                    items { id operationName isSuccess }
                    totalCount
                }
            }
            """;
        var (responseData, responseString) = await client.PostGqlRequest(query);
        responseString.ShouldNotContain("errors");
        responseData["data"]["auditLogs"]["totalCount"].GetValue<int>().ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task DeleteAuditLogsShouldWork()
    {
        string logId;
        using (var scope = ScopedService.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var log = new AuditLog
            {
                OperationName = "deleteMe",
                OperationType = OperationType.Mutation,
                IsSuccess = true,
            };
            uow.Attach(log);
            await uow.SaveChanges();
            logId = log.Id;
        }

        var client = SuperAdminClient;
        var mutation = """
            mutation($ids: [String!]!) {
                deleteAuditLogs(request: { ids: $ids })
            }
            """;
        var (responseData, responseString) = await client.PostGqlRequest(mutation, new { ids = new[] { logId } });
        responseString.ShouldNotContain("errors");
        responseData["data"]["deleteAuditLogs"].GetValue<long>().ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task EnabledOperationIsPersistedBeforeResponseCompletes()
    {
        var operation = "auditSuccess" + Guid.NewGuid().ToString("N");
        var marker = "value-" + Guid.NewGuid().ToString("N");
        var (response, body) = await SuperAdminClient.PostGqlRequest(
            $"mutation {operation}($marker: String!) {{ auditWriteSuccess(marker: $marker) }}", new { marker });
        body.ShouldNotContain("errors");
        response["data"]["auditWriteSuccess"].GetValue<string>().ShouldBe(marker);

        var rows = ReadOperationLogs(operation);
        rows.Length.ShouldBe(1);
        rows[0].IsSuccess.ShouldBeTrue();
        rows[0].OperationType.ShouldBe(OperationType.Mutation);
        rows[0].Variables!["marker"]!.GetValue<string>().ShouldBe(marker);
        rows[0].Result!.GetValue<string>().ShouldBe(marker);
    }

    [Fact]
    public async Task ExplicitlyDisabledOperationDoesNotCreateLog()
    {
        var operation = "auditDisabled" + Guid.NewGuid().ToString("N");
        var (_, body) = await SuperAdminClient.PostGqlRequest(
            $"mutation {operation} {{ auditWriteDisabled(marker: \"disabled\") }}");
        body.ShouldNotContain("errors");
        ReadOperationLogs(operation).ShouldBeEmpty();
    }

    [Fact]
    public async Task ExceptionIsPersistedAsFailedOperation()
    {
        var operation = "auditFailure" + Guid.NewGuid().ToString("N");
        var marker = "failure-" + Guid.NewGuid().ToString("N");
        var (_, body) = await SuperAdminClient.PostGqlRequest(
            $"mutation {operation}($marker: String!) {{ auditWriteFailure(marker: $marker) }}", new { marker });
        body.ShouldContain("errors");
        var rows = ReadOperationLogs(operation);
        rows.Length.ShouldBe(1);
        rows[0].IsSuccess.ShouldBeFalse();
        rows[0].Result!.ToJsonString().ShouldContain(marker);
    }

    [Fact]
    public async Task ReportedErrorIsReturnedToClientAndStoredInFailureDetails()
    {
        var operation = "auditReported" + Guid.NewGuid().ToString("N");
        var marker = "reported-" + Guid.NewGuid().ToString("N");
        var (_, body) = await SuperAdminClient.PostGqlRequest(
            $"mutation {operation}($marker: String!) {{ auditWriteReportedFailure(marker: $marker) }}", new { marker });
        body.ShouldContain("AUDIT_TEST_REPORTED_ERROR");
        body.ShouldContain(marker);
        var rows = ReadOperationLogs(operation);
        rows.Length.ShouldBe(1);
        rows[0].IsSuccess.ShouldBeFalse();
        rows[0].Result!.ToJsonString().ShouldContain(marker);
        rows[0].Result!.ToJsonString().ShouldContain("AUDIT_TEST_REPORTED_ERROR");
    }

    [Fact]
    public async Task ReportedErrorDoesNotContaminateAnotherFieldInSameOperation()
    {
        var operation = "auditMixed" + Guid.NewGuid().ToString("N");
        var marker = "mixed-" + Guid.NewGuid().ToString("N");
        var (response, body) = await SuperAdminClient.PostGqlRequest(
            $"mutation {operation}($marker: String!) {{ failed: auditWriteReportedFailure(marker: $marker) succeeded: auditWriteSuccess(marker: $marker) }}",
            new { marker });
        body.ShouldContain("AUDIT_TEST_REPORTED_ERROR");
        response["data"]["succeeded"].GetValue<string>().ShouldBe(marker);
        var rows = ReadOperationLogs(operation);
        rows.Length.ShouldBe(2);
        var success = rows.Single(x => x.IsSuccess);
        success.Result!.GetValue<string>().ShouldBe(marker);
        rows.Single(x => !x.IsSuccess).Result!.ToJsonString().ShouldContain("AUDIT_TEST_REPORTED_ERROR");
    }

    private AuditLog[] ReadOperationLogs(string operation)
    {
        using var scope = ScopedService.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IUnitOfWork>().Query<AuditLog>()
            .Where(x => x.OperationName == operation).ToArray();
    }
}
