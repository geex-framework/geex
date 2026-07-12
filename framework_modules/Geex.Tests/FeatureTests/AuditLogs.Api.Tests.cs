using Geex.Extensions.AuditLogs;
using Geex.Extensions.AuditLogs.Core.Entities;
using Geex.Extensions.AuditLogs.Requests;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using Shouldly;

namespace Geex.Tests.FeatureTests;

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
}
