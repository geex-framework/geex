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
using Geex.Extensions.Settings.Core;
using Geex.Extensions.Authorization;
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
        await As(user, null, work => Favorite.Add(work, Reference(host)));
        await As(user, tenantA, work => Favorite.Add(work, Reference(flowA)));
        await As(user, tenantB, work => Favorite.Add(work, Reference(flowB)));
        Assert.Equal(host, Assert.Single(await As(user, null, async work => (await work.Favorites(Type)).ToArray())).EntityId);
        Assert.Equal(flowA, Assert.Single(await As(user, tenantA, async work => (await work.Favorites(Type)).ToArray())).EntityId);
        Assert.Empty(await As(other, tenantA, async work => (await work.Favorites(Type)).ToArray()));
        await As(user, tenantA, work => Favorite.Add(work, Reference(flowB)));
        var unavailable = await As(user, tenantA, async work => (await work.Favorites(Type)).Single(item => item.EntityId == flowB));
        Assert.Equal(flowB, unavailable.DisplayValue);
        Assert.True(await As(user, tenantA, work => Favorite.Remove(work, Reference(flowB))));
        await As(other, tenantA, work => Favorite.Remove(work, Reference(flowA)));
        Assert.True(Assert.Single(await As(user, tenantA, work => work.FavoriteStatuses(Type, [flowA]))).IsFavorite);
    }

    [Fact]
    public async Task Host_can_switch_tenant_and_cancel_only_its_host_favorite_in_the_same_unit_of_work()
    {
        await Configure(Configuration());
        var userId = await CreateUser();
        var tenantCode = "favorites_switch_" + ObjectId.GenerateNewId();
        var id = await CreateFlow(tenantCode, "Shared reference");
        var reference = Reference(id);
        var tenantFavorite = await As(userId, null, async work =>
        {
            var tenant = work.ServiceProvider.GetRequiredService<ICurrentTenant>();
            await Favorite.Add(work, reference);
            FavoriteStatus saved;
            using (tenant.Change(tenantCode))
            {
                saved = await Favorite.Add(work, reference);
            }
            await work.SaveChanges();

            Assert.Equal(id, Assert.Single((await work.Favorites(Type)).ToArray()).EntityId);
            await Favorite.Remove(work, reference);
            using (tenant.Change(tenantCode))
            {
                Assert.True(Assert.Single(await work.FavoriteStatuses(Type, [id])).IsFavorite);
                var repeated = await Favorite.Add(work, reference);
                Assert.Equal(saved.CreatedOn, repeated.CreatedOn);
            }
            return saved;
        });

        Assert.Empty(await As(userId, null, async context => (await context.Favorites(Type)).ToArray()));
        var remaining = Assert.Single(await As(userId, tenantCode, async context => (await context.Favorites(Type)).ToArray()));
        Assert.Equal(id, remaining.EntityId);
        Assert.Equal(tenantFavorite.CreatedOn, remaining.CreatedOn);
    }

    [Fact]
    public async Task Settings_override_whole_arrays_and_keep_disabled_records()
    {
        var tenant = "favorites_settings_" + ObjectId.GenerateNewId();
        var user = ObjectId.GenerateNewId().ToString();
        var id = await CreateFlow(tenant, "Configured");
        await Configure(Configuration());
        Assert.Single(await As(user, tenant, work => FavoritesSettings.EntityTypes.GetEffectiveTypes(work)));
        await As(user, tenant, work => Favorite.Add(work, Reference(id)));
        await Configure(new JsonArray(), tenant);
        Assert.Empty(await As(user, tenant, work => FavoritesSettings.EntityTypes.GetEffectiveTypes(work)));
        Assert.Empty(await As(user, tenant, async work => (await work.Favorites(Type)).ToArray()));
        Assert.Single(await As(user, null, work => FavoritesSettings.EntityTypes.GetEffectiveTypes(work)));
        await Assert.ThrowsAsync<BusinessException>(() => As(user, tenant, work => Favorite.Add(work, Reference(id))));
        await Configure(Configuration("Description", null), tenant);
        var restored = Assert.Single(await As(user, tenant, async work => (await work.Favorites(Type)).ToArray()));
        Assert.Equal("Configured", restored.DisplayValue);
        Assert.Null(restored.DetailUrl);
        await Configure(new JsonArray());
        Assert.Empty(await As(user, null, work => FavoritesSettings.EntityTypes.GetEffectiveTypes(work)));
        Assert.Single(await As(user, tenant, work => FavoritesSettings.EntityTypes.GetEffectiveTypes(work)));
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
        Assert.Single(await As(GeexConstants.SuperAdminId, null, work => FavoritesSettings.EntityTypes.GetEffectiveTypes(work)));
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
        var types = await As(user, null, work => FavoritesSettings.EntityTypes.GetEffectiveTypes(work));
        Assert.Equal(["ApprovalFlowReview", "ApprovalFlow"], types.Select(type => type.EntityType.Value).ToArray());
        await As(user, null, work => Favorite.Add(work, new FavoriteReference { EntityType = FavoriteTestEntityTypes.Review, EntityId = id }));
        Assert.Empty(await As(user, null, async work => (await work.Favorites(Type)).ToArray()));
        await As(user, null, work => Favorite.Add(work, Reference(id)));
        Assert.Equal("Multiple types", Assert.Single(await As(user, null, async work => (await work.Favorites(Type)).ToArray())).DisplayValue);
        Assert.Equal("Live description", Assert.Single(await As(user, null, async work => (await work.Favorites(FavoriteTestEntityTypes.Review)).ToArray())).DisplayValue);
        await Configure(Configuration());
    }

    [Fact]
    public async Task Stored_titles_and_counts_survive_rename_access_loss_and_physical_deletion()
    {
        await Configure(Configuration());
        var user = await CreateUser();
        var id = await CreateFlow(null, "Original");
        await As(user, null, work => Favorite.Add(work, Reference(id)));
        await EditFlow(id, flow => flow.Name = "Renamed");
        var item = Assert.Single(await As(user, null, async work => (await work.Favorites(Type)).ToArray()));
        Assert.Equal("Original", item.DisplayValue);
        Assert.Equal($"/approval-flows/inbox/{id}", item.DetailUrl);
        await EditFlow(id, flow => { flow.Name = " "; flow.OrgCode = "unavailable"; });
        Assert.True(Assert.Single(await As(user, null, work => work.FavoriteStatuses(Type, [id]))).IsFavorite);
        Assert.Equal("Original", Assert.Single(await As(user, null, async work => (await work.Favorites(Type)).ToArray())).DisplayValue);
        using var scope = RootService.CreateScope();
        var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        using var filters = work.DbContext.DisableAllDataFilters();
        await work.DeleteAsync<ApprovalFlow>(id);
        await work.SaveChanges();
        Assert.Equal("Original", Assert.Single(await As(user, null, async read => (await read.Favorites(Type)).ToArray())).DisplayValue);
        await As(user, null, write => Favorite.Remove(write, Reference(id)));
        Assert.Equal(0, await DB.Collection<Favorite>().CountDocumentsAsync(favorite => favorite.EntityId == id));
    }

    [Fact]
    public async Task GraphQL_filters_and_sorts_snapshots_before_paging_including_revoked_targets()
    {
        await Configure(Configuration());
        var user = await CreateUser();
        var ids = new List<string>();
        for (var i = 0; i < 4; i++)
        {
            var id = await CreateFlow(null, $"Page {i}");
            ids.Add(id);
            await As(user, null, work => Favorite.Add(work, Reference(id)));
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
        Assert.Equal(3, response["data"]!["favorites"]!["totalCount"]!.GetValue<int>());
        Assert.Equal(ids[2], response["data"]!["favorites"]!["items"]![0]!["entityId"]!.GetValue<string>());
        var ascending = await Execute(client, """
            { favorites(entityType: ApprovalFlow, skip: 2, take: 1, sort: [{ createdOn: ascend }, { id: ascend }]) {
                totalCount items { entityId }
            } }
            """);
        AssertSuccess(ascending);
        Assert.Equal(4, ascending["data"]!["favorites"]!["totalCount"]!.GetValue<int>());
        Assert.Equal(ids[2], ascending["data"]!["favorites"]!["items"]![0]!["entityId"]!.GetValue<string>());
    }

    [Fact]
    public async Task Legacy_or_unreadable_targets_display_their_id()
    {
        await Configure(Configuration());
        var user = await CreateUser();
        var id = await CreateFlow(null, "First title");
        var first = await As(user, null, work => Favorite.Add(work, Reference(id)));
        await DB.Collection<Favorite>().UpdateOneAsync(item => item.UserId == user && item.EntityId == id, Builders<Favorite>.Update.Unset(item => item.Title));
        var legacy = Assert.Single(await As(user, null, async work => (await work.Favorites(Type)).ToArray()));
        Assert.Equal(id, legacy.DisplayValue);
        Assert.Equal(first.CreatedOn, legacy.CreatedOn);
        var missing = ObjectId.GenerateNewId().ToString();
        await As(user, null, work => Favorite.Add(work, Reference(missing)));
        var fallback = await As(user, null, async work => (await work.Favorites(Type)).Single(item => item.EntityId == missing));
        Assert.Equal(missing, fallback.DisplayValue);
        Assert.True(await As(user, null, work => Favorite.Remove(work, Reference(missing))));
    }

    [Fact]
    public async Task Concurrent_first_adds_keep_the_committed_snapshot_and_a_retry_reads_it()
    {
        await Configure(Configuration());
        var user = await CreateUser();
        var reference = Reference(await CreateFlow(null, "Pending title"));

        var conflict = await Assert.ThrowsAsync<MongoBulkWriteException<Favorite>>(() => As(user, null, async pending =>
        {
            var attempted = await Favorite.Add(pending, reference);
            await EditFlow(reference.EntityId, flow => flow.Name = "Committed title");
            await As(user, null, other => Favorite.Add(other, reference));
            return attempted;
        }));
        Assert.Contains(conflict.WriteErrors, error => error.Category == ServerErrorCategory.DuplicateKey);

        var records = DB.Collection<Favorite>().Find(item => item.UserId == user && item.EntityId == reference.EntityId);
        var saved = await records.SingleAsync();
        Assert.Equal("Committed title", saved.Title);
        await EditFlow(reference.EntityId, flow => flow.Name = "Later title");
        var retry = await As(user, null, work => Favorite.Add(work, reference));
        Assert.Equal(saved.CreatedOn, retry.CreatedOn);
        var afterRetry = await records.SingleAsync();
        Assert.Equal(saved.Id, afterRetry.Id);
        Assert.Equal(saved.Title, afterRetry.Title);
    }

    [Fact]
    public async Task A_favorite_becomes_visible_when_the_outer_transaction_commits()
    {
        await Configure(Configuration());
        var user = await CreateUser();
        var reference = Reference(await CreateFlow(null, "Outer transaction"));
        var records = DB.Collection<Favorite>().Find(item => item.UserId == user);
        var status = await As(user, null, async work =>
        {
            await using var transaction = work.StartExplicitTransaction();
            var first = await Favorite.Add(work, reference);
            await work.SaveChanges();
            Assert.Empty(await records.ToListAsync());
            await work.CommitExplicitTransaction();
            return first;
        });
        Assert.Equal(status.CreatedOn, (await records.SingleAsync()).CreatedOn);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Removing_and_readding_in_one_unit_of_work_creates_a_new_snapshot(bool supportTransaction)
    {
        await Configure(Configuration());
        var user = await CreateUser();
        var reference = Reference(await CreateFlow(null, "First title"));
        await As(user, null, work => Favorite.Add(work, reference));
        var favorites = DB.Collection<Favorite>().Find(item => item.UserId == user);
        var first = await favorites.SingleAsync();
        await EditFlow(reference.EntityId, flow => flow.Name = "Updated title");

        var status = await As(user, null, async work =>
        {
            work.DbContext.SupportTransaction = supportTransaction;
            await Favorite.Remove(work, reference);
            Assert.False(Assert.Single(await work.FavoriteStatuses(Type, [reference.EntityId])).IsFavorite);
            var added = await Favorite.Add(work, reference);
            Assert.Equal(added.CreatedOn, (await Favorite.Add(work, reference)).CreatedOn);
            await Favorite.Remove(work, reference);
            return await Favorite.Add(work, reference);
        });
        var saved = await favorites.SingleAsync();
        Assert.NotEqual(first.Id, saved.Id);
        Assert.Equal("Updated title", saved.Title);
        Assert.Equal(status.CreatedOn, saved.CreatedOn);
    }

    [Fact]
    public async Task Failed_multi_field_mutation_rolls_back_the_add_or_removal()
    {
        await Configure(Configuration());
        using var client = SuperAdminClient;
        var id = await CreateFlow(null, "Atomic mutation");
        var add = $"addFavorite(request: {{ entityType: ApprovalFlow, entityId: \"{id}\" }}) {{ isFavorite }}";
        var remove = $"removeFavorite(request: {{ entityType: ApprovalFlow, entityId: \"{id}\" }})";
        const string invalid = "invalid: addFavorite(request: { entityType: ApprovalFlow, entityId: \"\" }) { isFavorite }";
        Assert.NotNull((await Execute(client, $"mutation {{ {add} {invalid} }}"))["errors"]);
        Assert.Equal(0, await DB.Collection<Favorite>().CountDocumentsAsync(item => item.EntityId == id));
        AssertSuccess(await Execute(client, $"mutation {{ {add} }}"));
        Assert.NotNull((await Execute(client, $"mutation {{ {remove} {invalid} }}"))["errors"]);
        Assert.Equal(1, await DB.Collection<Favorite>().CountDocumentsAsync(item => item.EntityId == id));
    }

    [Fact]
    public async Task Setting_writes_share_the_transaction_and_rollback_cache_with_other_mutation_fields()
    {
        await Configure(Configuration());
        using var client = SuperAdminClient;
        var id = await CreateFlow(null, "Same operation");
        var response = await Execute(client, $$"""
            mutation {
                editSetting(request: { name: Favorites_EntityTypes, scope: Global, value: [] }) { name }
                addFavorite(request: { entityType: ApprovalFlow, entityId: "{{id}}" }) { isFavorite }
            }
            """);
        Assert.NotNull(response["errors"]);
        Assert.Single(await As(GeexConstants.SuperAdminId, null, work => FavoritesSettings.EntityTypes.GetEffectiveTypes(work)));
        Assert.Equal(0, await DB.Collection<Favorite>().CountDocumentsAsync(item => item.EntityId == id));
    }

    [Fact]
    public async Task Authorized_host_and_current_tenant_can_manage_only_their_intended_overrides()
    {
        await Configure(Configuration());
        var user = await CreateUser();
        var tenant = "managed_" + ObjectId.GenerateNewId();
        Task<ISetting> Set(IUnitOfWork work, string key) => work.GetSettingService()!
            .SetAsync(FavoritesSettings.EntityTypes, SettingScopeEnumeration.Tenant, key, new JsonArray());
        await Assert.ThrowsAsync<BusinessException>(() => As(user, null, work => Set(work, tenant)));
        await Assert.ThrowsAsync<BusinessException>(() => As(user, tenant, work => Set(work, "other")));
        using var scope = RootService.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IRbacEnforcer>().SetPermissionsAsync(user, [SettingsPermission.Edit.Value]);
        await As(user, null, work => Set(work, tenant));
        Assert.Empty(await As(user, tenant, work => FavoritesSettings.EntityTypes.GetEffectiveTypes(work)));
        await As(user, null, work => work.GetSettingService()!.RestoreTenantInheritanceAsync(FavoritesSettings.EntityTypes, tenant));
        Assert.Single(await As(user, tenant, work => FavoritesSettings.EntityTypes.GetEffectiveTypes(work)));
    }

    [Fact]
    public async Task Invalid_historical_scopes_are_ignored_and_only_invalid_type_entries_are_disabled()
    {
        await Configure(Configuration());
        var user = await CreateUser();
        var tenant = "historical_" + ObjectId.GenerateNewId();
        var stored = Configuration().AsArray();
        stored.Insert(0, new JsonObject { ["EntityType"] = "Missing", ["NameField"] = "Name" });
        var legacy = new Setting(FavoritesSettings.EntityTypes, new JsonArray(), SettingScopeEnumeration.User, user);
        var partial = new Setting(FavoritesSettings.EntityTypes, stored, SettingScopeEnumeration.Tenant, tenant);
        using var scope = RootService.CreateScope();
        var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        work.Attach(legacy);
        work.Attach(partial);
        await work.SaveChanges();
        await As(user, tenant, async context =>
        {
            var service = context.GetSettingService()!;
            var single = await service.GetSetting(FavoritesSettings.EntityTypes);
            var bulk = (await service.GetActiveSettings()).Single(item => item.Name == FavoritesSettings.EntityTypes);
            Assert.Equal(SettingScopeEnumeration.Tenant, single.Scope);
            Assert.True(JsonNode.DeepEquals(single.Value, bulk.Value));
            Assert.Equal(Type, Assert.Single(await FavoritesSettings.EntityTypes.GetEffectiveTypes(context)).EntityType);
            return true;
        });
        Assert.Equal(1, await DB.Collection<Setting>().CountDocumentsAsync(item => item.Id == legacy.Id));
    }

    [Fact]
    public async Task Custom_authorized_reader_provides_the_title_or_falls_back_to_id()
    {
        var user = await CreateUser();
        var id = await CreateFlow(null, "Custom title");
        var type = FavoriteTestEntityTypes.Review;
        var reference = new FavoriteReference { EntityType = type, EntityId = id };
        foreach (var (field, expected) in new[] { ("DisplayLabel", "CUSTOM TITLE"), ("RestrictedLabel", id) })
        {
            await Configure(new JsonArray(new JsonObject { ["EntityType"] = type.Value, ["NameField"] = field }));
            await As(user, null, work => Favorite.Add(work, reference));
            Assert.Equal(expected, Assert.Single(await As(user, null, async work => (await work.Favorites(type)).ToArray())).DisplayValue);
            await As(user, null, work => Favorite.Remove(work, reference));
        }
        await Configure(Configuration());
    }

    private static FavoriteReference Reference(string id) => new() { EntityType = Type, EntityId = id };
    private async Task<T> As<T>(string user, string? tenant, Func<IUnitOfWork, Task<T>> action, params string[] orgCodes)
    {
        using var scope = RootService.CreateScope();
        using var actor = scope.ServiceProvider.GetRequiredService<ICurrentUser>().Change(user);
        ((CurrentUser)scope.ServiceProvider.GetRequiredService<ICurrentUser>())._claimsIdentity = new System.Security.Claims.ClaimsIdentity(
            orgCodes.Select(code => new System.Security.Claims.Claim(GeexClaimType._Org, code)), "Test");
        using var tenancy = scope.ServiceProvider.GetRequiredService<ICurrentTenant>().Change(tenant);
        var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var result = await action(work);
        await work.SaveChanges();
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

public sealed class FavoriteTestEntityTypes : FavoriteEntityType
{
    private FavoriteTestEntityTypes(string value, Type entityType) : base(value, entityType)
    {
        RegisterTitleField("DisplayLabel", (_, entity, _) => Task.FromResult<string?>(((ApprovalFlow)entity).Name.ToUpperInvariant()));
        RegisterTitleField("RestrictedLabel", (_, _, _) => throw new UnauthorizedAccessException());
    }

    public static FavoriteEntityType Review { get; } = new FavoriteTestEntityTypes("ApprovalFlowReview", typeof(ApprovalFlow));
}
