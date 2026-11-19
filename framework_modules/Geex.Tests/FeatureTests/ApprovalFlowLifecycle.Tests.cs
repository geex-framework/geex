using Geex.Extensions.ApprovalFlows;
using Geex.Extensions.ApprovalFlows.Core.Entities;
using Geex.Extensions.ApprovalFlows.Core.Migrations;
using Geex.Extensions.ApprovalFlows.Gql;
using Geex.Extensions.ApprovalFlows.Requests;
using Geex.Extensions.Authentication;
using Geex.Extensions.Identity;
using Geex.Extensions.Identity.Requests;
using Geex.Storage;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Bson;
using MongoDB.Driver;
using Volo.Abp;

namespace Geex.Tests.FeatureTests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ApprovalFlowLifecycleCollection : ICollectionFixture<ApprovalFlowLifecycleFactory>
{
    public const string Name = "Approval flow lifecycle";
}

public sealed class ApprovalFlowLifecycleFactory : WebApplicationFactory<Program>
{
    protected override IHostBuilder CreateHostBuilder()
    {
        var mongo = Environment.GetEnvironmentVariable("GEEX_APPROVAL_TEST_MONGO")
            ?? throw new InvalidOperationException("GEEX_APPROVAL_TEST_MONGO is required.");
        var url = MongoUrl.Create(mongo);
        if (url.Server.Host != "127.0.0.1" || !url.DatabaseName.StartsWith("geex_approval_", StringComparison.Ordinal))
            throw new InvalidOperationException("Approval lifecycle tests require an isolated local database.");
        var redisPort = Environment.GetEnvironmentVariable("GEEX_APPROVAL_TEST_REDIS_PORT")
            ?? throw new InvalidOperationException("GEEX_APPROVAL_TEST_REDIS_PORT is required.");
        return Program.CreateHostBuilder()
            .ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["GeexCoreModuleOptions:ConnectionString"] = mongo,
                ["GeexCoreModuleOptions:AutoMigration"] = "true",
                ["GeexCoreModuleOptions:Redis:Hosts:0:Host"] = "127.0.0.1",
                ["GeexCoreModuleOptions:Redis:Hosts:0:Port"] = redisPort,
                ["GeexCoreModuleOptions:Redis:Password"] = "",
                ["GeexCoreModuleOptions:Redis:Database"] = "0",
                ["BackgroundJobModuleOptions:Disabled"] = "true",
            }));
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = builder.Build();
        host.Start();
        return host;
    }
}

[Collection(ApprovalFlowLifecycleCollection.Name)]
public sealed class ApprovalFlowLifecycleTests(ApprovalFlowLifecycleFactory factory) : TestsBase(factory)
{
    [Fact]
    public async Task Cancel_mutation_is_rejected_by_the_public_schema()
    {
        var response = await Execute(SuperAdminClient,
            "mutation { cancelApprovalFlow(id: \"000000000000000000000099\") }");
        Assert.NotNull(response["errors"]);
        Assert.Contains("cancelApprovalFlow", response["errors"]!.ToJsonString());
        Assert.Null(response["data"]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Creator_and_unrelated_user_cannot_close_another_approvers_node(bool creator)
    {
        var setup = await CreateFlow();
        var user = creator ? setup.Creator : await CreateUser();
        var response = await Execute(UserClient(user),
            $"mutation {{ closeApprovalFlow(nodeId: \"{setup.NodeId}\") }}");
        Assert.NotNull(response["errors"]);
        Assert.Contains(response["errors"]!.AsArray(), error =>
            error!["extensions"]?["message"]?.GetValue<string>() == "仅当前激活节点的审批人可关闭流程.");
        using var scope = RootService.CreateScope();
        var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        using var filters = work.DbContext.DisableAllDataFilters();
        var flow = work.Query<ApprovalFlow>().Single(x => x.Id == setup.FlowId);
        Assert.Equal(ApprovalFlowStatus.Processing, flow.Status);
        Assert.Null(flow.ArchivedAt);
        Assert.Empty(flow.Nodes.SelectMany(x => x.ChatLogs).Where(x => x.LogType == ApprovalFlowNodeLogType.FlowClosed));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Current_approver_closes_and_archives_once(bool creatorIsApprover, bool consulting)
    {
        var setup = await CreateFlow(creatorIsApprover, consulting);
        var client = UserClient(setup.Approver);
        var operation = $"mutation {{ closeApprovalFlow(nodeId: \"{setup.NodeId}\") }}";
        var response = await Execute(client, operation);
        Assert.Null(response["errors"]);
        Assert.True(response["data"]!["closeApprovalFlow"]!.GetValue<bool>());
        DateTimeOffset? archived;
        using (var scope = RootService.CreateScope())
        {
            var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            using var filters = work.DbContext.DisableAllDataFilters();
            var flow = work.Query<ApprovalFlow>().Single(x => x.Id == setup.FlowId);
            Assert.Equal(ApprovalFlowStatus.Closed, flow.Status);
            Assert.NotNull(flow.ArchivedAt);
            archived = flow.ArchivedAt;
            Assert.False(flow.CanEditForm);
            Assert.False(flow.ActiveNode!.CanOperate);
            var log = Assert.Single(flow.Nodes.SelectMany(x => x.ChatLogs).Where(x => x.LogType == ApprovalFlowNodeLogType.FlowClosed));
            Assert.Equal(setup.ApproverId, log.FromUserId);
            Assert.Equal("关闭审批流程并归档", log.Message);
        }
        var repeated = await Execute(client, operation);
        Assert.NotNull(repeated["errors"]);
        using var verify = RootService.CreateScope();
        var persisted = verify.ServiceProvider.GetRequiredService<IUnitOfWork>();
        using var visibility = persisted.DbContext.DisableAllDataFilters();
        var reloaded = persisted.Query<ApprovalFlow>().Single(x => x.Id == setup.FlowId);
        Assert.Equal(archived, reloaded.ArchivedAt);
        Assert.Single(reloaded.Nodes.SelectMany(x => x.ChatLogs).Where(x => x.LogType == ApprovalFlowNodeLogType.FlowClosed));
    }

    [Fact]
    public async Task Closing_unsubmits_the_entity_and_replacement_preserves_the_closed_result()
    {
        var setup = await CreateFlow(associated: true);
        var closed = await Execute(UserClient(setup.Approver),
            $"mutation {{ closeApprovalFlow(nodeId: \"{setup.NodeId}\") }}");
        Assert.Null(closed["errors"]);
        DateTimeOffset? archived;
        using (var scope = RootService.CreateScope())
        {
            var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            using var filters = work.DbContext.DisableAllDataFilters();
            var entity = work.Query<LifecycleApprovalEntity>().Single(x => x.Id == setup.EntityId);
            Assert.Equal(ApproveStatus.Default, entity.ApproveStatus);
            archived = work.Query<ApprovalFlow>().Single(x => x.Id == setup.FlowId).ArchivedAt;
            Assert.Null(new ApprovalFlowQuery(work).ApprovalFlowByEntity(LifecycleAssociatedEntityType.Lifecycle, setup.EntityId!));
        }
        var replacement = await Execute(UserClient(setup.Creator),
            "mutation($request: CreateApprovalFlowRequest!) { createApprovalFlow(request: $request) { id status } }",
            new { request = new {
                name = "Replacement", associatedEntityId = setup.EntityId, associatedEntityType = "Lifecycle",
                nodes = new[] { new { name = "Review", auditUserId = setup.ApproverId } },
            } });
        Assert.Null(replacement["errors"]);
        var replacementId = replacement["data"]!["createApprovalFlow"]!["id"]!.GetValue<string>();
        using var verify = RootService.CreateScope();
        var persisted = verify.ServiceProvider.GetRequiredService<IUnitOfWork>();
        using var visibility = persisted.DbContext.DisableAllDataFilters();
        var previous = persisted.Query<ApprovalFlow>().Single(x => x.Id == setup.FlowId);
        Assert.Equal(ApprovalFlowStatus.Closed, previous.Status);
        Assert.Equal(archived, previous.ArchivedAt);
        Assert.Equal(replacementId, previous.ReplacedByFlowId);
        Assert.Equal(replacementId, new ApprovalFlowQuery(persisted)
            .ApprovalFlowByEntity(LifecycleAssociatedEntityType.Lifecycle, setup.EntityId!)!.Id);
        Assert.Throws<UserFriendlyException>(() => previous.Archive(ObjectId.GenerateNewId().ToString()));
        Assert.Equal(replacementId, previous.ReplacedByFlowId);
    }

    [Fact]
    public async Task Archive_filter_is_independent_of_the_flow_result()
    {
        var setup = await CreateFlow();
        using var scope = RootService.CreateScope();
        var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        using var visibility = work.DbContext.DisableAllDataFilters();
        var flow = work.Query<ApprovalFlow>().Single(x => x.Id == setup.FlowId);
        flow.Status = ApprovalFlowStatus.Finished;
        flow.ArchivedAt = DateTimeOffset.UtcNow;
        await work.SaveChanges();
        using var verify = RootService.CreateScope();
        var persisted = verify.ServiceProvider.GetRequiredService<IUnitOfWork>();
        using var filters = persisted.DbContext.DisableAllDataFilters();
        var query = new ApprovalFlowQuery(persisted);
        var reloaded = persisted.Query<ApprovalFlow>().Single(x => x.Id == setup.FlowId);
        Assert.Equal(ApprovalFlowStatus.Finished, reloaded.Status);
        Assert.NotNull(reloaded.ArchivedAt);
        var archived = await query.ApprovalFlow(new QueryApprovalFlowRequest { Status = ApprovalFlowStatus.Finished, Archived = true });
        var current = await query.ApprovalFlow(new QueryApprovalFlowRequest { Status = ApprovalFlowStatus.Finished, Archived = false });
        Assert.Contains(archived.ToList(), x => x.Id == setup.FlowId);
        Assert.DoesNotContain(current.ToList(), x => x.Id == setup.FlowId);
    }

    [Fact]
    public async Task Status_migration_preserves_archive_relationship_and_audit_fields_and_is_idempotent()
    {
        using var scope = RootService.CreateScope();
        var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var collection = work.DbContext.DefaultDb.GetCollection<BsonDocument>(nameof(ApprovalFlow));
        var documents = new BsonValue[] { -1, 2, "Canceled", "Archived", 0, 1 }.Select(status => new BsonDocument
        {
            ["_id"] = ObjectId.GenerateNewId(), ["Status"] = status,
            ["ArchivedAt"] = new BsonDateTime(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc)),
            ["ReplacedByFlowId"] = ObjectId.GenerateNewId().ToString(),
            ["CreatorUserId"] = ObjectId.GenerateNewId().ToString(),
            ["FormValues"] = new BsonArray { new BsonDocument { ["Key"] = "reason", ["Values"] = new BsonArray { "Original reason" } } },
        }).ToArray();
        await collection.InsertManyAsync(documents);
        var migration = new _639288672498274456_approval_flow_closed_status();
        await migration.UpgradeAsync(work);
        await migration.UpgradeAsync(work);
        for (var index = 0; index < documents.Length; index++)
        {
            var actual = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", documents[index]["_id"])).SingleAsync();
            var expected = documents[index].DeepClone().AsBsonDocument;
            expected["Status"] = index < 4 ? 2 : index - 4;
            Assert.Equal(expected, actual);
        }
    }

    private async Task<string> CreateUser()
    {
        var username = "flow_" + ObjectId.GenerateNewId();
        using var scope = RootService.CreateScope();
        var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await work.Request(new CreateUserRequest
        {
            Username = username, Password = "Approval-test-123!", Nickname = username,
            IsEnable = true, RoleIds = [], OrgCodes = [],
        });
        await work.SaveChanges();
        return username;
    }

    private async Task<Setup> CreateFlow(bool creatorIsApprover = false, bool consulting = false, bool associated = false)
    {
        var creator = await CreateUser();
        var approver = creatorIsApprover ? creator : await CreateUser();
        using var scope = RootService.CreateScope();
        var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        using var visibility = work.DbContext.DisableAllDataFilters();
        var creatorId = work.Query<IUser>().Single(x => x.Username == creator).Id;
        var approverId = work.Query<IUser>().Single(x => x.Username == approver).Id;
        using var actor = scope.ServiceProvider.GetRequiredService<ICurrentUser>().Change(creatorId);
        var entity = associated ? work.Attach(new LifecycleApprovalEntity()) : null;
        var flow = await new ApprovalFlowMutation(work).CreateApprovalFlow(new CreateApprovalFlowRequest
        {
            Name = "Lifecycle " + ObjectId.GenerateNewId(),
            AssociatedEntityId = entity?.Id,
            AssociatedEntityType = associated ? LifecycleAssociatedEntityType.Lifecycle : null,
            Nodes = [new ApprovalFlowNodeData { Name = "Review", AuditUserId = approverId }],
        });
        if (consulting) flow.ActiveNode!.NodeStatus |= ApprovalFlowNodeStatus.Consulting;
        await work.SaveChanges();
        return new Setup(flow.Id, flow.ActiveNode!.Id, creator, creatorId, approver, approverId, entity?.Id);
    }

    private sealed record Setup(string FlowId, string NodeId, string Creator, string CreatorId, string Approver, string ApproverId, string? EntityId);

    private static async Task<JsonNode> Execute(HttpClient client, string query, object? variables = null,
        [CallerMemberName] string scenario = "")
    {
        var started = DateTimeOffset.UtcNow;
        using var response = await client.PostAsJsonAsync("/graphql", new { query, variables });
        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        var evidence = Environment.GetEnvironmentVariable("GEEX_APPROVAL_TEST_EVIDENCE");
        if (!string.IsNullOrEmpty(evidence))
        {
            Directory.CreateDirectory(evidence);
            await File.WriteAllTextAsync(Path.Combine(evidence, Guid.NewGuid().ToString("N") + ".json"),
                JsonSerializer.Serialize(new { scenario, query, variables, status = (int)response.StatusCode,
                    startedAt = started, finishedAt = DateTimeOffset.UtcNow, response = body }));
        }
        return body;
    }
}

public class LifecycleApprovalEntity : Entity<LifecycleApprovalEntity>, IApproveEntity
{
    public ApproveStatus ApproveStatus { get; set; }
    public string? ApproveRemark { get; set; }
    public bool Submittable => ApproveStatus == ApproveStatus.Default;
}

public class LifecycleAssociatedEntityType(string value, Type type) : ApprovalFlowAssociatedEntityType(value, type)
{
    public static LifecycleAssociatedEntityType Lifecycle { get; } = new("Lifecycle", typeof(LifecycleApprovalEntity));
}
