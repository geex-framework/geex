using System.Text.Json;
using System.Text.Json.Nodes;
using Geex.Extensions.ApprovalFlows;
using Geex.Extensions.ApprovalFlows.Gql;
using Geex.Extensions.Authorization;
using Geex.Extensions.Identity;
using Geex.Extensions.Identity.Requests;
using Geex.Extensions.Settings;
using Geex.MultiTenant;
using Microsoft.Extensions.DependencyInjection;
using Path = System.IO.Path;

namespace Geex.Tests.FeatureTests;

[Collection(nameof(TestsCollection))]
public class FlowSubmitConfigApiTests(TestApplicationFactory factory) : TestsBase(factory)
{
    private const string SaveMutation = "mutation($value:String!){saveFlowConfig(value:$value)}";
    private JsonNode? _originalConfig;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        using var scope = ScopedService.CreateScope();
        var settings = scope.ServiceProvider.GetRequiredService<ISettingService>();
        _originalConfig = (await settings.GetSetting(ApprovalFlowSettings.FlowSubmitConfig)).Value?.DeepClone();
        await SaveInScope("{}");
    }

    public override async Task DisposeAsync()
    {
        try
        {
            using var scope = ScopedService.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ISettingService>().SetAsync(
                ApprovalFlowSettings.FlowSubmitConfig, SettingScopeEnumeration.Global, null, _originalConfig);
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChanges();
        }
        finally { await base.DisposeAsync(); }
    }

    [Fact]
    public async Task EmptyConfigurationDisablesUnconfiguredModules()
    {
        var definition = ApprovalFlowSettings.FlowSubmitConfig;
        Assert.Equal("FlowSubmitConfig", definition.Name);
        Assert.Equal("{}", definition.DefaultValue.ToJsonString());
        Assert.Equal(new[] { SettingScopeEnumeration.Global, SettingScopeEnumeration.Tenant }, definition.ValidScopes);
        var query = "query{flowConfig flowSubmitConfig}";
        var (response, _) = await SuperAdminClient.PostGqlRequest(query);
        Assert.Null(response["errors"]);
        Assert.Equal("{}", response["data"]!["flowConfig"]!.GetValue<string>());
        Assert.Equal("{}", response["data"]!["flowSubmitConfig"]!.GetValue<string>());
        using var scope = ScopedService.CreateScope();
        Assert.False(await FlowSubmitGate.IsEnabled(scope.ServiceProvider.GetRequiredService<IUnitOfWork>(), "unconfigured"));
        await Evidence(nameof(EmptyConfigurationDisablesUnconfiguredModules), new { query }, response);
    }

    [Theory]
    [InlineData("{\"workReports\":{\"useFlow\":true,\"defaultTag\":\"work\"}}", true, false)]
    [InlineData("{\"investReports\":{\"useFlow\":true,\"defaultTag\":null}}", false, true)]
    [InlineData("{\"workReports\":{\"useFlow\":false,\"defaultTag\":\"work\"},\"investReports\":{\"useFlow\":true,\"defaultTag\":\"invest\"}}", false, true)]
    public async Task SaveAndReadKeepModuleKeysIndependent(string value, bool workEnabled, bool investEnabled)
    {
        var (saved, _) = await SuperAdminClient.PostGqlRequest(SaveMutation, new { value });
        Assert.Null(saved["errors"]);
        Assert.True(saved["data"]!["saveFlowConfig"]!.GetValue<bool>());
        var (response, _) = await SuperAdminClient.PostGqlRequest("query{flowConfig flowSubmitConfig}");
        Assert.Null(response["errors"]);
        foreach (var field in new[] { "flowConfig", "flowSubmitConfig" })
            Assert.True(JsonNode.DeepEquals(JsonNode.Parse(value), JsonNode.Parse(response["data"]![field]!.GetValue<string>())));
        using var scope = ScopedService.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        Assert.Equal(workEnabled, await FlowSubmitGate.IsEnabled(uow, "workReports"));
        Assert.Equal(investEnabled, await FlowSubmitGate.IsEnabled(uow, "investReports"));
        Assert.False(await FlowSubmitGate.IsEnabled(uow, "otherModule"));
        var stored = Assert.Single(uow.Query<ISetting>().Where(x => x.Name == ApprovalFlowSettings.FlowSubmitConfig
            && x.Scope == SettingScopeEnumeration.Global).ToList());
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(value), stored.Value));
        await Evidence(nameof(SaveAndReadKeepModuleKeysIndependent) + "-" + workEnabled + "-" + value.Length,
            new { query = SaveMutation, variables = new { value } }, new { saved, response, workEnabled, investEnabled });
    }

    [Fact]
    public async Task InvalidConfigurationIsRejectedWithoutChangingStoredValue()
    {
        const string valid = "{\"assets\":{\"useFlow\":true,\"defaultTag\":\"asset\"}}";
        await SaveInScope(valid);
        var observations = new List<object>();
        foreach (var value in new[] { "broken", "null", "[]", "{\"assets\":true}",
            "{\"assets\":{\"useFlow\":\"true\"}}", "{\"assets\":{\"useFlow\":true,\"defaultTag\":1}}" })
        {
            var (response, _) = await SuperAdminClient.PostGqlRequest(SaveMutation, new { value }, ignoreError: true);
            Assert.NotEmpty(response["errors"]!.AsArray());
            Assert.Contains(response["errors"]!.AsArray(), x => x!["message"]!.GetValue<string>().Contains("流程配置须为模块对象"));
            Assert.True(JsonNode.DeepEquals(JsonNode.Parse(valid), JsonNode.Parse(await ReadInScope())));
            observations.Add(new { value, response });
        }
        await Evidence(nameof(InvalidConfigurationIsRejectedWithoutChangingStoredValue), new { query = SaveMutation }, observations);
    }

    [Fact]
    public async Task RuntimeReadRequiresLoginAndManagementKeepsSeparatePermissions()
    {
        var username = "flow-reader-" + Guid.NewGuid().ToString("N");
        string userId;
        using (var scope = ScopedService.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var user = await uow.Request(new CreateUserRequest
            {
                Username = username, Nickname = username, Password = "FlowReader123!", IsEnable = true,
                RoleIds = [], OrgCodes = [],
            });
            await uow.SaveChanges();
            userId = user.Id;
        }
        using var client = UserClient(username);
        var (runtime, _) = await client.PostGqlRequest("query{flowSubmitConfig}");
        Assert.Null(runtime["errors"]);
        Assert.Equal("{}", runtime["data"]!["flowSubmitConfig"]!.GetValue<string>());
        var (deniedRead, _) = await client.PostGqlRequest("query{flowConfig}", ignoreError: true);
        AssertDenied(deniedRead);
        using (var scope = ScopedService.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IRbacEnforcer>()
                .SetPermissionsAsync(userId, [ApprovalFlowPermission.QueryFlowConfig.Value]);
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChanges();
        }
        var (managedRead, _) = await client.PostGqlRequest("query{flowConfig}");
        Assert.Null(managedRead["errors"]);
        Assert.Equal("{}", managedRead["data"]!["flowConfig"]!.GetValue<string>());
        var value = "{\"assets\":{\"useFlow\":true}}";
        var (deniedWrite, _) = await client.PostGqlRequest(SaveMutation, new { value }, ignoreError: true);
        AssertDenied(deniedWrite);
        var (anonymous, _) = await AnonymousClient.PostGqlRequest("query{flowSubmitConfig}", ignoreError: true);
        AssertDenied(anonymous);
        Assert.Equal("{}", await ReadInScope());
        await Evidence(nameof(RuntimeReadRequiresLoginAndManagementKeepsSeparatePermissions),
            new { query = SaveMutation, variables = new { value } }, new { runtime, deniedRead, managedRead, deniedWrite, anonymous });
    }

    [Fact]
    public async Task TenantConfigurationOverridesWholeObjectWithoutChangingGlobalOrOtherTenant()
    {
        var tenantA = "flow-a-" + Guid.NewGuid().ToString("N");
        var tenantB = "flow-b-" + Guid.NewGuid().ToString("N");
        const string global = "{\"workReports\":{\"useFlow\":true,\"defaultTag\":\"global\"}}";
        const string local = "{\"investReports\":{\"useFlow\":true,\"defaultTag\":null}}";
        await SaveInScope(global);
        await SaveInScope(local, tenantA);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(local), JsonNode.Parse(await ReadInScope(tenantA))));
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(global), JsonNode.Parse(await ReadInScope(tenantB))));
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(global), JsonNode.Parse(await ReadInScope())));
        using var scope = ScopedService.CreateScope();
        using var tenant = scope.ServiceProvider.GetRequiredService<ICurrentTenant>().Change(tenantA);
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        Assert.False(await FlowSubmitGate.IsEnabled(uow, "workReports"));
        Assert.True(await FlowSubmitGate.IsEnabled(uow, "investReports"));
        var rows = uow.Query<ISetting>().Where(x => x.Name == ApprovalFlowSettings.FlowSubmitConfig).ToList();
        var localRow = Assert.Single(rows.Where(x => x.Scope == SettingScopeEnumeration.Tenant && x.ScopedKey == tenantA));
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(local), localRow.Value));
        Assert.DoesNotContain(rows, x => x.Scope == SettingScopeEnumeration.Tenant && x.ScopedKey == tenantB);
        await Evidence(nameof(TenantConfigurationOverridesWholeObjectWithoutChangingGlobalOrOtherTenant),
            new { tenantA, tenantB, global, local }, rows.Select(x => new { scope = x.Scope.Value, x.ScopedKey, x.Value }));
    }

    private async Task SaveInScope(string value, string? tenantCode = null)
    {
        using var scope = ScopedService.CreateScope();
        using var tenant = scope.ServiceProvider.GetRequiredService<ICurrentTenant>().Change(tenantCode);
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        Assert.True(await new ApprovalFlowMutation(uow).SaveFlowConfig(value));
        await uow.SaveChanges();
    }

    private async Task<string> ReadInScope(string? tenantCode = null)
    {
        using var scope = ScopedService.CreateScope();
        using var tenant = scope.ServiceProvider.GetRequiredService<ICurrentTenant>().Change(tenantCode);
        return await new ApprovalFlowQuery(scope.ServiceProvider.GetRequiredService<IUnitOfWork>()).FlowSubmitConfig();
    }

    private static void AssertDenied(JsonNode response)
    {
        var errors = response["errors"]!.AsArray();
        Assert.NotEmpty(errors);
        Assert.Contains(errors, x => x?["extensions"]?["code"]?.GetValue<string>()
            is "AUTH_NOT_AUTHENTICATED" or "AUTH_NOT_AUTHORIZED");
    }

    private static async Task Evidence(string testCase, object request, object response)
    {
        var root = Environment.GetEnvironmentVariable("PP_BACKEND_EVIDENCE");
        if (string.IsNullOrWhiteSpace(root)) return;
        var directory = Path.Combine(root, "cases");
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, $"flow-config-{testCase}.json"),
            JsonSerializer.Serialize(new { testCase, observations = new[] { new { request, response } } }));
    }
}
