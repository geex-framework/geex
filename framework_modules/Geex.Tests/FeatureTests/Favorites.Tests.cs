using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Geex.Extensions.ApprovalFlows.Core.Entities;
using Geex.Extensions.ApprovalFlows;
using Geex.Extensions.ApprovalFlows.Requests;
using Geex.Extensions.Authentication;
using Geex.Extensions.Favorites;
using Geex.Extensions.Favorites.Core;
using Geex.Extensions.Identity.Requests;
using Geex.Extensions.Settings;
using Geex.MultiTenant;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Entities;

namespace Geex.Tests.FeatureTests;

[Collection(ApprovalFlowLifecycleCollection.Name)]
public sealed class FavoritesTests(ApprovalFlowLifecycleFactory factory) : TestsBase(factory)
{
    private static FavoriteEntityType Type => FavoriteEntityType.List.Single(type => type.Value == "ApprovalFlow");
    private static JsonNode Configuration(string name = "Name", string? route = "/approval-flows/inbox/{id}")
        => new JsonArray(new JsonObject { ["EntityType"] = "ApprovalFlow", ["NameField"] = name, ["DetailRoute"] = route });

    [Fact]
    public async Task GraphQL_contract_is_authenticated_and_round_trips_idempotent_favorites()
    {
        await Configure(Configuration());
        var executor = await RootService.GetRequiredService<IRequestExecutorResolver>().GetRequestExecutorAsync();
        var evidence = Environment.GetEnvironmentVariable("GEEX_FAVORITES_TEST_EVIDENCE");
        if (!string.IsNullOrEmpty(evidence)) await File.WriteAllTextAsync(Path.Combine(evidence, "schema.graphql"), executor.Schema.ToString());
        using var anonymous = AnonymousClient;
        Assert.NotNull((await Execute(anonymous, "{ favoriteEntityTypes { entityType } }"))["errors"]);
        using var client = SuperAdminClient;
        var types = await Execute(client, "{ favoriteEntityTypes { entityType nameField detailRoute } }");
        AssertSuccess(types);
        Assert.Equal("ApprovalFlow", types["data"]!["favoriteEntityTypes"]![0]!["entityType"]!.GetValue<string>());
        var flowId = await CreateFlow(null, "API favorite");
        var add = $"mutation {{ addFavorite(request: {{ entityType: ApprovalFlow, entityId: \"{flowId}\" }}) {{ isFavorite createdOn }} }}";
        var first = await Execute(client, add);
        AssertSuccess(first);
        var second = await Execute(client, add);
        AssertSuccess(second);
        Assert.Equal(first["data"]!["addFavorite"]!["createdOn"]!.ToJsonString(), second["data"]!["addFavorite"]!["createdOn"]!.ToJsonString());
        var statuses = await Execute(client, $"{{ favoriteStatuses(entityType: ApprovalFlow, entityIds: [\"{flowId}\"]) {{ isFavorite }} }}");
        AssertSuccess(statuses);
        Assert.True(statuses["data"]!["favoriteStatuses"]![0]!["isFavorite"]!.GetValue<bool>());
        AssertSuccess(await Execute(client, "{ favorites(entityType: ApprovalFlow, take: 10) { items { entityId } } }"));
        AssertSuccess(await Execute(client, add));
        Assert.Equal(1, await DB.Collection<Favorite>().CountDocumentsAsync(item => item.EntityId == flowId && item.UserId == GeexConstants.SuperAdminId));
        var remove = $"mutation {{ removeFavorite(request: {{ entityType: ApprovalFlow, entityId: \"{flowId}\" }}) }}";
        AssertSuccess(await Execute(client, remove));
        AssertSuccess(await Execute(client, remove));
        var readded = await Execute(client, add);
        AssertSuccess(readded);
        Assert.NotEqual(first["data"]!["addFavorite"]!["createdOn"]!.ToJsonString(), readded["data"]!["addFavorite"]!["createdOn"]!.ToJsonString());
    }

    [Fact]
    public async Task Favorites_are_isolated_by_user_and_tenant_including_host_context()
    {
        await Configure(Configuration());
        var user = ObjectId.GenerateNewId().ToString();
        var other = ObjectId.GenerateNewId().ToString();
        var host = await CreateFlow(null, "Host");
        var tenantA = "favorites_a_" + ObjectId.GenerateNewId();
        var tenantB = "favorites_b_" + ObjectId.GenerateNewId();
        var flowA = await CreateFlow(tenantA, "Tenant A");
        var flowB = await CreateFlow(tenantB, "Tenant B");
        await As(user, null, service => service.Add(Reference(host)));
        await As(user, tenantA, service => service.Add(Reference(flowA)));
        await As(user, tenantB, service => service.Add(Reference(flowB)));
        Assert.Equal(host, Assert.Single(await As(user, null, async service => (await service.Query(Type)).ToArray())).EntityId);
        Assert.Equal(flowA, Assert.Single(await As(user, tenantA, async service => (await service.Query(Type)).ToArray())).EntityId);
        Assert.Empty(await As(other, tenantA, async service => (await service.Query(Type)).ToArray()));
        await Assert.ThrowsAsync<BusinessException>(() => As(user, null, service => service.Add(Reference(flowA))));
        await Assert.ThrowsAsync<BusinessException>(() => As(user, tenantA, service => service.Add(Reference(flowB))));
        await As(other, tenantA, service => service.Remove(Reference(flowA)));
        Assert.True(Assert.Single(await As(user, tenantA, service => service.Statuses(Type, [flowA]))).IsFavorite);
    }

    [Fact]
    public async Task Settings_override_whole_arrays_and_keep_disabled_records()
    {
        var tenant = "favorites_settings_" + ObjectId.GenerateNewId();
        var user = ObjectId.GenerateNewId().ToString();
        var id = await CreateFlow(tenant, "Configured");
        await Configure(Configuration());
        Assert.Single(await As(user, tenant, service => service.GetTypes()));
        await As(user, tenant, service => service.Add(Reference(id)));
        await Configure(new JsonArray(), tenant);
        Assert.Empty(await As(user, tenant, service => service.GetTypes()));
        Assert.Empty(await As(user, tenant, async service => (await service.Query(Type)).ToArray()));
        Assert.Single(await As(user, null, service => service.GetTypes()));
        await Assert.ThrowsAsync<BusinessException>(() => As(user, tenant, service => service.Add(Reference(id))));
        await Configure(Configuration("Description", null), tenant);
        var restored = Assert.Single(await As(user, tenant, async service => (await service.Query(Type)).ToArray()));
        Assert.Equal("Live description", restored.DisplayValue);
        Assert.Null(restored.DetailUrl);
        await Configure(new JsonArray());
        Assert.Empty(await As(user, null, service => service.GetTypes()));
        Assert.Single(await As(user, tenant, service => service.GetTypes()));
        await Configure(Configuration());
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("[{\"EntityType\":\"Missing\",\"NameField\":\"Name\"}]")]
    [InlineData("[{\"EntityType\":\"ApprovalFlow\",\"NameField\":\"Missing\"}]")]
    [InlineData("[{\"EntityType\":\"ApprovalFlow\",\"NameField\":\"CreatedOn\"}]")]
    [InlineData("[{\"EntityType\":\"ApprovalFlow\",\"NameField\":\"Name\",\"DetailRoute\":\"https://example.com/{id}\"}]")]
    [InlineData("[{\"EntityType\":\"ApprovalFlow\",\"NameField\":\"Name\"},{\"EntityType\":\"ApprovalFlow\",\"NameField\":\"Name\"}]")]
    public async Task Invalid_settings_are_rejected_before_persistence(string json)
    {
        await Configure(Configuration());
        await Assert.ThrowsAsync<BusinessException>(() => Configure(JsonNode.Parse(json)!));
        Assert.Single(await As(GeexConstants.SuperAdminId, null, service => service.GetTypes()));
    }

    [Fact]
    public async Task Settings_edits_update_one_record_and_survive_cache_eviction()
    {
        using var client = SuperAdminClient;
        foreach (var configuration in new JsonNode[] { Configuration(), new JsonArray(), Configuration("Description", null) })
        {
            using var httpResponse = await client.PostAsJsonAsync("/graphql", new {
                query = "mutation($value: Any) { editSetting(request: { name: Favorites_EntityTypes, scope: Global, value: $value }) { name } }",
                variables = new { value = configuration }
            });
            var response = JsonNode.Parse(await httpResponse.Content.ReadAsStringAsync())!;
            AssertSuccess(response);
            using var scope = RootService.CreateScope();
            var redis = scope.ServiceProvider.GetRequiredService<StackExchange.Redis.Extensions.Core.Abstractions.IRedisDatabase>();
            await redis.RemoveAsync("Setting:Global:Favorites_EntityTypes");
            var settings = scope.ServiceProvider.GetRequiredService<ISettingService>();
            var stored = (await settings.GetGlobalSettings()).Single(item => item.Name == FavoritesSettings.EntityTypes);
            Assert.True(JsonNode.DeepEquals(configuration, stored.Value));
            Assert.Equal(1, await DB.Collection<Geex.Extensions.Settings.Core.Setting>().CountDocumentsAsync(
                item => item.Name == FavoritesSettings.EntityTypes && item.Scope == SettingScopeEnumeration.Global && item.ScopedKey == null));
            var types = await Execute(client, "{ favoriteEntityTypes { entityType } }");
            AssertSuccess(types);
            Assert.Equal(configuration.AsArray().Count, types["data"]!["favoriteEntityTypes"]!.AsArray().Count);
        }
        await Configure(Configuration());
    }

    [Fact]
    public async Task Application_types_keep_configured_order_and_independent_favorites()
    {
        await Configure(new JsonArray(
            new JsonObject { ["EntityType"] = "ApprovalFlowReview", ["NameField"] = "Description" },
            new JsonObject { ["EntityType"] = "ApprovalFlow", ["NameField"] = "Name" }));
        var user = await CreateUser();
        var id = await CreateFlow(null, "Multiple types");
        var types = await As(user, null, service => service.GetTypes());
        Assert.Equal(["ApprovalFlowReview", "ApprovalFlow"], types.Select(type => type.EntityType.Value).ToArray());
        await As(user, null, service => service.Add(new FavoriteReference { EntityType = FavoriteTestEntityTypes.Review, EntityId = id }));
        Assert.Empty(await As(user, null, async service => (await service.Query(Type)).ToArray()));
        Assert.Equal("Live description", Assert.Single(await As(user, null, async service => (await service.Query(FavoriteTestEntityTypes.Review)).ToArray())).DisplayValue);
        await Configure(Configuration());
    }

    [Fact]
    public async Task Names_are_live_and_unreadable_records_are_hidden_until_access_returns_and_deleted_targets_are_cleaned()
    {
        await Configure(Configuration());
        var user = await CreateUser();
        var id = await CreateFlow(null, "Original");
        await As(user, null, service => service.Add(Reference(id)));
        await EditFlow(id, flow => flow.Name = "Renamed");
        var item = Assert.Single(await As(user, null, async service => (await service.Query(Type)).ToArray()));
        Assert.Equal("Renamed", item.DisplayValue);
        Assert.Equal($"/approval-flows/inbox/{id}", item.DetailUrl);
        await EditFlow(id, flow => { flow.Name = " "; flow.OrgCode = "unavailable"; });
        Assert.Empty(await As(user, null, async service => (await service.Query(Type)).ToArray()));
        Assert.False(Assert.Single(await As(user, null, service => service.Statuses(Type, [id]))).IsFavorite);
        Assert.Equal(id, Assert.Single(await As(user, null, async service => (await service.Query(Type)).ToArray(), "unavailable")).DisplayValue);
        using var scope = RootService.CreateScope();
        var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        using var filters = work.DbContext.DisableAllDataFilters();
        await work.DeleteAsync<ApprovalFlow>(id);
        await work.SaveChanges();
        Assert.Equal(0, await DB.Collection<Favorite>().CountDocumentsAsync(favorite => favorite.EntityId == id));
    }

    [Fact]
    public async Task GraphQL_filters_and_sorts_before_paging_and_excludes_hidden_records_from_count()
    {
        await Configure(Configuration());
        var user = await CreateUser();
        var ids = new List<string>();
        for (var i = 0; i < 4; i++)
        {
            var id = await CreateFlow(null, $"Page {i}");
            ids.Add(id);
            await As(user, null, service => service.Add(Reference(id)));
            await DB.Collection<Favorite>().UpdateOneAsync(favorite => favorite.EntityId == id && favorite.UserId == user,
                Builders<Favorite>.Update.Set(favorite => favorite.CreatedOn, new DateTimeOffset(2026, 11, 20 + i, 12, 0, 0, TimeSpan.Zero)));
        }
        await EditFlow(ids[3], flow => flow.OrgCode = "unavailable");
        using var scope = RootService.CreateScope();
        var username = scope.ServiceProvider.GetRequiredService<IUnitOfWork>().Query<Geex.Extensions.Identity.IUser>().Single(item => item.Id == user).Username;
        using var client = UserClient(username);
        var response = await Execute(client, """
            { favorites(entityType: ApprovalFlow, skip: 1, take: 1,
                filter: { createdOn: { gte: "2026-11-21T00:00:00Z", lt: "2026-11-24T00:00:00Z" } },
                sort: [{ createdOn: descend }, { id: descend }]) { totalCount items { entityId createdOn } } }
            """);
        AssertSuccess(response);
        Assert.Equal(2, response["data"]!["favorites"]!["totalCount"]!.GetValue<int>());
        Assert.Equal(ids[1], response["data"]!["favorites"]!["items"]![0]!["entityId"]!.GetValue<string>());
    }

    [Fact]
    public async Task Broad_deletion_events_keep_favorites_when_the_target_still_exists()
    {
        await Configure(Configuration());
        var user = await CreateUser();
        var id = await CreateFlow(null, "Same ID in another collection");
        await As(user, null, service => service.Add(Reference(id)));
        await EditFlow(id, flow => flow.OrgCode = "unavailable");
        using var scope = RootService.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<MediatX.IEventHandler<Geex.Notifications.EntityDeletedEvent<IEntityBase>>>();
        await handler.Handle(new Geex.Notifications.EntityDeletedEvent<IEntityBase>(id), default);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChanges();
        Assert.Equal(1, await DB.Collection<Favorite>().CountDocumentsAsync(favorite => favorite.EntityId == id && favorite.UserId == user));
    }

    private static FavoriteReference Reference(string id) => new() { EntityType = Type, EntityId = id };
    private async Task<T> As<T>(string user, string? tenant, Func<FavoriteService, Task<T>> action, params string[] orgCodes)
    {
        using var scope = RootService.CreateScope();
        using var actor = scope.ServiceProvider.GetRequiredService<ICurrentUser>().Change(user);
        ((CurrentUser)scope.ServiceProvider.GetRequiredService<ICurrentUser>())._claimsIdentity = new System.Security.Claims.ClaimsIdentity(
            orgCodes.Select(code => new System.Security.Claims.Claim(GeexClaimType._Org, code)), "Test");
        using var tenancy = scope.ServiceProvider.GetRequiredService<ICurrentTenant>().Change(tenant);
        var result = await action(scope.ServiceProvider.GetRequiredService<FavoriteService>());
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChanges();
        return result;
    }
    private async Task Configure(JsonNode value, string? tenant = null)
    {
        using var scope = RootService.CreateScope();
        using var actor = scope.ServiceProvider.GetRequiredService<ICurrentUser>().Change(GeexConstants.SuperAdminId);
        var settings = scope.ServiceProvider.GetRequiredService<ISettingService>();
        await settings.SetAsync(FavoritesSettings.EntityTypes, tenant == null ? SettingScopeEnumeration.Global : SettingScopeEnumeration.Tenant, tenant, value);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChanges();
    }
    private async Task<string> CreateUser()
    {
        using var scope = RootService.CreateScope();
        var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var user = await work.Request(new CreateUserRequest { Username = "favorite_" + ObjectId.GenerateNewId(),
            Password = "Favorites-test-123!", IsEnable = true, RoleIds = [], OrgCodes = [] });
        await work.SaveChanges();
        return user.Id;
    }
    private async Task<string> CreateFlow(string? tenant, string name)
    {
        var user = await CreateUser();
        using var scope = RootService.CreateScope();
        var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        using var filters = work.DbContext.DisableAllDataFilters();
        using var actor = scope.ServiceProvider.GetRequiredService<ICurrentUser>().Change(user);
        var flow = new ApprovalFlow(new CreateApprovalFlowRequest { Name = name, Description = "Live description",
            Nodes = [new ApprovalFlowNodeData { Name = "Review", AuditUserId = user }] }, work) { TenantCode = tenant };
        await work.SaveChanges();
        return flow.Id;
    }
    private async Task EditFlow(string id, Action<ApprovalFlow> edit)
    {
        using var scope = RootService.CreateScope();
        var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        using var filters = work.DbContext.DisableAllDataFilters();
        edit(work.Query<ApprovalFlow>().Single(flow => flow.Id == id));
        await work.SaveChanges();
    }
    private static async Task<JsonNode> Execute(HttpClient client, string query)
    {
        using var response = await client.PostAsJsonAsync("/graphql", new { query });
        return JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
    }
    private static void AssertSuccess(JsonNode response) => Assert.True(response["errors"] == null, response.ToJsonString());
}

public sealed class FavoriteTestEntityTypes(string value, Type entityType) : FavoriteEntityType(value, entityType)
{
    public static FavoriteEntityType Review { get; } = new FavoriteTestEntityTypes("ApprovalFlowReview", typeof(ApprovalFlow));
}
