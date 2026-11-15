using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Path = System.IO.Path;
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
        var operation = "auditQuery" + Guid.NewGuid().ToString("N");
        using (var scope = ScopedService.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            uow.Attach(new AuditLog
            {
                Id = logId,
                OperationName = operation,
                OperationType = OperationType.Mutation,
                IsSuccess = true,
                OperatorId = GeexConstants.SuperAdminId,
            });
            await uow.SaveChanges();
        }

        var client = SuperAdminClient;
        var query = """
            query($id: String!) {
                auditLogs(filter: { id: { eq: $id } }, take: 10) {
                    items { id operationName isSuccess }
                    totalCount
                }
            }
            """;
        var variables = new { id = logId };
        var (responseData, responseString) = await client.PostGqlRequest(query, variables);
        responseString.ShouldNotContain("errors");
        responseData["data"]["auditLogs"]["totalCount"].GetValue<int>().ShouldBe(1);
        responseData["data"]["auditLogs"]["items"]!.AsArray().Single()!["id"]!.GetValue<string>().ShouldBe(logId);
        await WriteEvidence(nameof(QueryAuditLogsShouldWork), new { query, variables }, responseData, ReadOperationLogs(operation));
    }

    [Fact]
    public async Task DeleteAuditLogsShouldWork()
    {
        string logId;
        var operation = "auditDelete" + Guid.NewGuid().ToString("N");
        using (var scope = ScopedService.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var log = new AuditLog
            {
                OperationName = operation,
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
        var variables = new { ids = new[] { logId } };
        var (responseData, responseString) = await client.PostGqlRequest(mutation, variables);
        responseString.ShouldNotContain("errors");
        responseData["data"]["deleteAuditLogs"].GetValue<long>().ShouldBeGreaterThan(0);
        var rows = ReadOperationLogs(operation);
        rows.ShouldBeEmpty();
        await WriteEvidence(nameof(DeleteAuditLogsShouldWork), new { query = mutation, variables }, responseData, rows);
    }

    [Fact]
    public async Task EnabledOperationIsPersistedBeforeResponseCompletes()
    {
        var operation = "auditSuccess" + Guid.NewGuid().ToString("N");
        var marker = "value-" + Guid.NewGuid().ToString("N");
        var query = $"mutation {operation}($marker: String!) {{ auditWriteSuccess(marker: $marker) }}";
        var variables = new { marker };
        var (response, body) = await SuperAdminClient.PostGqlRequest(query, variables);
        body.ShouldNotContain("errors");
        response["data"]["auditWriteSuccess"].GetValue<string>().ShouldBe(marker);

        var rows = ReadOperationLogs(operation);
        rows.Length.ShouldBe(1);
        rows[0].IsSuccess.ShouldBeTrue();
        rows[0].OperationType.ShouldBe(OperationType.Mutation);
        rows[0].Variables!["marker"]!.GetValue<string>().ShouldBe(marker);
        rows[0].Result!.GetValue<string>().ShouldBe(marker);
        await WriteEvidence(nameof(EnabledOperationIsPersistedBeforeResponseCompletes), new { query, variables }, response, rows);
    }

    [Fact]
    public async Task ExplicitlyDisabledOperationDoesNotCreateLog()
    {
        var operation = "auditDisabled" + Guid.NewGuid().ToString("N");
        var query = $"mutation {operation} {{ auditWriteDisabled(marker: \"disabled\") }}";
        var (response, body) = await SuperAdminClient.PostGqlRequest(query);
        body.ShouldNotContain("errors");
        var rows = ReadOperationLogs(operation);
        rows.ShouldBeEmpty();
        await WriteEvidence(nameof(ExplicitlyDisabledOperationDoesNotCreateLog), new { query }, response, rows);
    }

    [Fact]
    public async Task ExceptionIsPersistedAsFailedOperation()
    {
        var operation = "auditFailure" + Guid.NewGuid().ToString("N");
        var marker = "failure-" + Guid.NewGuid().ToString("N");
        var request = new
        {
            query = $"mutation {operation}($marker: String!) {{ auditWriteFailure(marker: $marker) }}",
            variables = new { marker },
        };
        using var response = await SuperAdminClient.PostAsJsonAsync("/graphql", request);
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        var error = body["errors"]!.AsArray().Single()!;
        error["path"]![0]!.GetValue<string>().ShouldBe("auditWriteFailure");
        error["extensions"]!["message"]!.GetValue<string>().ShouldBe(marker);
        var rows = ReadOperationLogs(operation);
        rows.Length.ShouldBe(1);
        rows[0].IsSuccess.ShouldBeFalse();
        rows[0].Result!.ToJsonString().ShouldContain(marker);
        await WriteEvidence(nameof(ExceptionIsPersistedAsFailedOperation), request, body, rows, (int)response.StatusCode);
    }

    [Fact]
    public async Task ReportedErrorIsReturnedToClientAndStoredInFailureDetails()
    {
        var operation = "auditReported" + Guid.NewGuid().ToString("N");
        var marker = "reported-" + Guid.NewGuid().ToString("N");
        var query = $"mutation {operation}($marker: String!) {{ auditWriteReportedFailure(marker: $marker) }}";
        var variables = new { marker };
        var (response, body) = await SuperAdminClient.PostGqlRequest(query, variables);
        body.ShouldContain("AUDIT_TEST_REPORTED_ERROR");
        body.ShouldContain(marker);
        var rows = ReadOperationLogs(operation);
        rows.Length.ShouldBe(1);
        rows[0].IsSuccess.ShouldBeFalse();
        rows[0].Result!.ToJsonString().ShouldContain(marker);
        rows[0].Result!.ToJsonString().ShouldContain("AUDIT_TEST_REPORTED_ERROR");
        await WriteEvidence(nameof(ReportedErrorIsReturnedToClientAndStoredInFailureDetails), new { query, variables }, response, rows);
    }

    [Fact]
    public async Task ReportedErrorDoesNotContaminateAnotherFieldInSameOperation()
    {
        var operation = "auditMixed" + Guid.NewGuid().ToString("N");
        var marker = "mixed-" + Guid.NewGuid().ToString("N");
        var query = $"mutation {operation}($marker: String!) {{ failed: auditWriteReportedFailure(marker: $marker) succeeded: auditWriteSuccess(marker: $marker) }}";
        var variables = new { marker };
        var (response, body) = await SuperAdminClient.PostGqlRequest(query, variables);
        body.ShouldContain("AUDIT_TEST_REPORTED_ERROR");
        response["data"]["succeeded"].GetValue<string>().ShouldBe(marker);
        var rows = ReadOperationLogs(operation);
        rows.Length.ShouldBe(2);
        var success = rows.Single(x => x.IsSuccess);
        success.Result!.GetValue<string>().ShouldBe(marker);
        rows.Single(x => !x.IsSuccess).Result!.ToJsonString().ShouldContain("AUDIT_TEST_REPORTED_ERROR");
        await WriteEvidence(nameof(ReportedErrorDoesNotContaminateAnotherFieldInSameOperation), new { query, variables }, response, rows);
    }

    private static async Task WriteEvidence(string testCase, object request, JsonNode response, AuditLog[] logs, int? status = null)
    {
        var root = Environment.GetEnvironmentVariable("PP_BACKEND_EVIDENCE");
        if (string.IsNullOrWhiteSpace(root)) return;
        var directory = Path.Combine(root, "cases");
        Directory.CreateDirectory(directory);
        var observations = new[]
        {
            new
            {
                endpoint = "/graphql", request, status, response,
                logCount = logs.Length,
                logs = logs.Select(log => new
                {
                    log.Id, log.OperationName, operationType = log.OperationType.ToString(), log.IsSuccess,
                    log.Variables, log.Result,
                }).ToArray(),
            },
        };
        await File.WriteAllTextAsync(Path.Combine(directory, $"audit-{testCase}.json"),
            JsonSerializer.Serialize(new { testCase, observations }));
    }

    private AuditLog[] ReadOperationLogs(string operation)
    {
        using var scope = ScopedService.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IUnitOfWork>().Query<AuditLog>()
            .Where(x => x.OperationName == operation).ToArray();
    }
}
