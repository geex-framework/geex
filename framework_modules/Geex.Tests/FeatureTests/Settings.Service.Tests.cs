using System.Text.Json.Nodes;
using Geex.Extensions.MultiTenant.Core.Aggregates.Tenants;
using Geex.Extensions.Requests.MultiTenant;
using Geex.Extensions.Settings;
using Geex.Extensions.Settings.Requests;
using Geex.MultiTenant;

using Microsoft.Extensions.DependencyInjection;

using MongoDB.Bson;

using Shouldly;

namespace Geex.Tests.FeatureTests
{
    [Collection(nameof(TestsCollection))]
    public class SettingsServiceTests : TestsBase
    {
        public SettingsServiceTests(TestApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task EditSettingServiceShouldWork()
        {
            // Arrange
            var testSettingName = TestModuleSettings.GlobalSetting;
            var testValue = ObjectId.GenerateNewId().ToString();

            // Act
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetService<IUnitOfWork>();
                var currentTenant = uow.ServiceProvider.GetService<ICurrentTenant>();
                currentTenant?.Change(null);

                var setting = await uow.Request(new EditSettingRequest
                {
                    Scope = SettingScopeEnumeration.Global,
                    ScopedKey = null,
                    Name = testSettingName,
                    Value = testValue
                });
                await uow.SaveChanges();

                // Assert
                setting.ShouldNotBeNull();
                setting.Name.ShouldBe(testSettingName);
                setting.Value.GetValue<string>().ShouldBe(testValue);
                setting.Scope.ShouldBe(SettingScopeEnumeration.Global);
            }
        }

        [Fact]
        public async Task GetSettingsServiceShouldWork()
        {
            // Arrange
            var testSettingName = TestModuleSettings.GlobalSetting;
            var testValue = ObjectId.GenerateNewId().ToString();

            // Create test setting
            using (var setupScope = ScopedService.CreateScope())
            {
                var setupUow = setupScope.ServiceProvider.GetService<IUnitOfWork>();
                var currentTenant = setupUow.ServiceProvider.GetService<ICurrentTenant>();
                currentTenant?.Change(null);

                await setupUow.Request(new EditSettingRequest
                {
                    Scope = SettingScopeEnumeration.Global,
                    ScopedKey = null,
                    Name = testSettingName,
                    Value = testValue
                });
                await setupUow.SaveChanges();
            }

            // Act & Assert
            using (var verifyScope = ScopedService.CreateScope())
            {
                var verifyUow = verifyScope.ServiceProvider.GetService<IUnitOfWork>();
                var settings = verifyUow.Query<ISetting>().Where(x => x.Name == testSettingName).ToList();

                settings.ShouldNotBeEmpty();
                settings.First().Name.ShouldBe(testSettingName);
                settings.First().Value.GetValue<string>().ShouldBe(testValue);
            }
        }

        [Fact]
        public async Task SettingWithDifferentScopesShouldWork()
        {
            // Arrange
            var testSettingName = TestModuleSettings.GlobalSetting;
            var globalValue = "GlobalValue_" + ObjectId.GenerateNewId();
            var tenantValue = "TenantValue_" + ObjectId.GenerateNewId();
            var userValue = "UserValue_" + ObjectId.GenerateNewId();
            var tenantCode = "test_tenant_" + ObjectId.GenerateNewId();
            var userId = ObjectId.GenerateNewId().ToString();

            // Act - Create tenant first
            using (var tenantScope = ScopedService.CreateScope())
            {
                var tenantUow = tenantScope.ServiceProvider.GetService<IUnitOfWork>();
                var newTenant = tenantUow.Create(new CreateTenantRequest()
                {
                    Code = tenantCode,
                    ExternalInfo = null,
                    Name = "Test Tenant"
                });
                await tenantUow.SaveChanges();
            }

            // Create global setting
            using (var globalScope = ScopedService.CreateScope())
            {
                var globalUow = globalScope.ServiceProvider.GetService<IUnitOfWork>();
                var currentTenant = globalUow.ServiceProvider.GetService<ICurrentTenant>();
                currentTenant?.Change(null);

                var globalSetting = await globalUow.Request(new EditSettingRequest
                {
                    Scope = SettingScopeEnumeration.Global,
                    ScopedKey = null,
                    Name = testSettingName,
                    Value = globalValue
                });
                await globalUow.SaveChanges();

                globalSetting.ShouldNotBeNull();
                globalSetting.Scope.ShouldBe(SettingScopeEnumeration.Global);
                globalSetting.Value.GetValue<string>().ShouldBe(globalValue);
            }

            // Create tenant setting
            using (var tenantScope = ScopedService.CreateScope())
            {
                var tenantUow = tenantScope.ServiceProvider.GetService<IUnitOfWork>();
                var currentTenant = tenantUow.ServiceProvider.GetService<ICurrentTenant>();
                currentTenant?.Change(tenantCode);

                var tenantSetting = await tenantUow.Request(new EditSettingRequest
                {
                    Scope = SettingScopeEnumeration.Tenant,
                    ScopedKey = tenantCode,
                    Name = testSettingName,
                    Value = tenantValue
                });
                await tenantUow.SaveChanges();

                tenantSetting.ShouldNotBeNull();
                tenantSetting.Scope.ShouldBe(SettingScopeEnumeration.Tenant);
                tenantSetting.Value.GetValue<string>().ShouldBe(tenantValue);
            }

            // Create user setting
            using (var userScope = ScopedService.CreateScope())
            {
                var userUow = userScope.ServiceProvider.GetService<IUnitOfWork>();
                var currentTenant = userUow.ServiceProvider.GetService<ICurrentTenant>();
                currentTenant?.Change(tenantCode);

                var userSetting = await userUow.Request(new EditSettingRequest
                {
                    Scope = SettingScopeEnumeration.User,
                    ScopedKey = userId,
                    Name = testSettingName,
                    Value = userValue
                });
                await userUow.SaveChanges();

                userSetting.ShouldNotBeNull();
                userSetting.Scope.ShouldBe(SettingScopeEnumeration.User);
                userSetting.Value.GetValue<string>().ShouldBe(userValue);
            }

            // Test 1: Verify global context returns global setting
            using (var globalTestScope = ScopedService.CreateScope())
            {
                var globalTestUow = globalTestScope.ServiceProvider.GetService<IUnitOfWork>();
                var currentTenant = globalTestUow.ServiceProvider.GetService<ICurrentTenant>();
                currentTenant?.Change(null);

                var globalSettings = await globalTestUow.Request(new GetSettingsRequest(SettingScopeEnumeration.Global)
                {
                    SettingDefinitions = [testSettingName]
                });

                var retrievedGlobalSetting = globalSettings.FirstOrDefault(x => x.Name == testSettingName);
                retrievedGlobalSetting.ShouldNotBeNull();
                retrievedGlobalSetting.Value.GetValue<string>().ShouldBe(globalValue);
                retrievedGlobalSetting.Scope.ShouldBe(SettingScopeEnumeration.Global);
            }

            // Test 2: Verify tenant context returns tenant setting (with global as fallback)
            using (var tenantTestScope = ScopedService.CreateScope())
            {
                var tenantTestUow = tenantTestScope.ServiceProvider.GetService<IUnitOfWork>();
                var currentTenant = tenantTestUow.ServiceProvider.GetService<ICurrentTenant>();
                currentTenant?.Change(tenantCode);

                var activeSettings = await tenantTestUow.Request(new GetInitSettingsRequest());
                var activeSetting = activeSettings.FirstOrDefault(x => x.Name == testSettingName);
                activeSetting.ShouldNotBeNull();
                // Should get tenant value since it has higher priority than global
                activeSetting.Value.GetValue<string>().ShouldBe(tenantValue);
                activeSetting.Scope.ShouldBe(SettingScopeEnumeration.Tenant);
            }

            // Test 3: Verify different tenant context returns global setting (no tenant-specific setting)
            var anotherTenantCode = "another_tenant_" + ObjectId.GenerateNewId();
            using (var anotherTenantScope = ScopedService.CreateScope())
            {
                var anotherTenantUow = anotherTenantScope.ServiceProvider.GetService<IUnitOfWork>();
                var newTenant = anotherTenantUow.Create(new CreateTenantRequest()
                {
                    Code = anotherTenantCode,
                    ExternalInfo = null,
                    Name = "Another Test Tenant"
                });
                await anotherTenantUow.SaveChanges();

                var currentTenant = anotherTenantUow.ServiceProvider.GetService<ICurrentTenant>();
                currentTenant?.Change(anotherTenantCode);

                var activeSettings = await anotherTenantUow.Request(new GetInitSettingsRequest());
                var activeSetting = activeSettings.FirstOrDefault(x => x.Name == testSettingName);
                activeSetting.ShouldNotBeNull();
                // Should get global value since no tenant-specific setting exists
                activeSetting.Value.GetValue<string>().ShouldBe(globalValue);
                activeSetting.Scope.ShouldBe(SettingScopeEnumeration.Global);
            }

            // Test 4: Verify all three settings exist independently in database
            using (var verifyScope = ScopedService.CreateScope())
            {
                var verifyUow = verifyScope.ServiceProvider.GetService<IUnitOfWork>();
                var allSettings = verifyUow.Query<ISetting>().Where(x => x.Name == testSettingName).ToList();

                allSettings.Count.ShouldBe(3);
                allSettings.Any(x => x.Scope == SettingScopeEnumeration.Global && x.Value.GetValue<string>() == globalValue).ShouldBeTrue();
                allSettings.Any(x => x.Scope == SettingScopeEnumeration.Tenant && x.ScopedKey == tenantCode && x.Value.GetValue<string>() == tenantValue).ShouldBeTrue();
                allSettings.Any(x => x.Scope == SettingScopeEnumeration.User && x.ScopedKey == userId && x.Value.GetValue<string>() == userValue).ShouldBeTrue();
            }
        }

        [Fact]
        public async Task ComplexSettingValueShouldWork()
        {
            // Arrange
            var testSettingName = TestModuleSettings.GlobalSetting;
            var navItem = new
            {
                text = "<span class=\"nav-group-text\">系统及配置</span>",
                icon = (string)null,
                shortcutRoot = false,
                link = (string)null,
                badge = 0,
                acl = new[] { "identity_query_orgs" },
                shortcut = false,
                i18n = (string)null,
                group = true,
                hideInBreadcrumb = true
            };

            // Act
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetService<IUnitOfWork>();
                var currentTenant = uow.ServiceProvider.GetService<ICurrentTenant>();
                currentTenant?.Change(null);

                var setting = await uow.Request(new EditSettingRequest
                {
                    Scope = SettingScopeEnumeration.Global,
                    ScopedKey = null,
                    Name = testSettingName,
                    Value = new JsonArray(JsonNode.Parse(navItem.ToJson()))
                });
                await uow.SaveChanges();

                // Assert
                setting.ShouldNotBeNull();
                setting.Name.ShouldBe(testSettingName);
                setting.Value.ShouldNotBeNull();
            }
        }

        [Fact]
        public async Task UpdateExistingSettingServiceShouldWork()
        {
            // Arrange
            var testSettingName = TestModuleSettings.GlobalSetting;
            var originalValue = ObjectId.GenerateNewId().ToString();
            var updatedValue = ObjectId.GenerateNewId().ToString();

            // Create initial setting
            using (var setupScope = ScopedService.CreateScope())
            {
                var setupUow = setupScope.ServiceProvider.GetService<IUnitOfWork>();
                var currentTenant = setupUow.ServiceProvider.GetService<ICurrentTenant>();
                currentTenant?.Change(null);

                await setupUow.Request(new EditSettingRequest
                {
                    Scope = SettingScopeEnumeration.Global,
                    ScopedKey = null,
                    Name = testSettingName,
                    Value = originalValue
                });
                await setupUow.SaveChanges();
            }

            // Act - Update the setting
            using (var updateScope = ScopedService.CreateScope())
            {
                var updateUow = updateScope.ServiceProvider.GetService<IUnitOfWork>();
                var currentTenant = updateUow.ServiceProvider.GetService<ICurrentTenant>();
                currentTenant?.Change(null);

                var updatedSetting = await updateUow.Request(new EditSettingRequest
                {
                    Scope = SettingScopeEnumeration.Global,
                    ScopedKey = null,
                    Name = testSettingName,
                    Value = updatedValue
                });
                await updateUow.SaveChanges();

                // Assert
                updatedSetting.ShouldNotBeNull();
                updatedSetting.Name.ShouldBe(testSettingName);
                updatedSetting.Value.GetValue<string>().ShouldBe(updatedValue);
            }

            // Verify only one setting exists with this name
            using (var verifyScope = ScopedService.CreateScope())
            {
                var verifyUow = verifyScope.ServiceProvider.GetService<IUnitOfWork>();
                var globalSettings = await verifyUow.GetSettingService().GetGlobalSettings();
                var allSettings = globalSettings.Where(x => x.Name == TestModuleSettings.GlobalSetting).ToList();
                allSettings.Count.ShouldBe(1);
                allSettings.First().Value.GetValue<string>().ShouldBe(updatedValue);
            }
        }

        [Fact]
        public async Task SettingWithScopedKeyShouldWork()
        {
            // Arrange
            var testSettingName = TestModuleSettings.TenantSetting;
            var tenantCode = "test";
            var testValue = ObjectId.GenerateNewId().ToString();

            // Act
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetService<IUnitOfWork>();

                var newTenant = uow.Create(new CreateTenantRequest()
                {
                    Code = tenantCode,
                    ExternalInfo = null,
                    Name = "testName"
                });

                var currentTenant = uow.ServiceProvider.GetService<ICurrentTenant>();
                currentTenant?.Change(tenantCode);

                var setting = await uow.Request(new EditSettingRequest
                {
                    Scope = SettingScopeEnumeration.Tenant,
                    ScopedKey = tenantCode,
                    Name = testSettingName,
                    Value = testValue
                });
                await uow.SaveChanges();

                // Assert
                setting.ShouldNotBeNull();
                setting.Name.ShouldBe(testSettingName);
                setting.ScopedKey.ShouldBe(tenantCode);
                setting.Scope.ShouldBe(SettingScopeEnumeration.Tenant);
                setting.Value.GetValue<string>().ShouldBe(testValue);
            }

            // Verify retrieval
            using (var verifyScope = ScopedService.CreateScope())
            {
                var verifyUow = verifyScope.ServiceProvider.GetService<IUnitOfWork>();
                var retrievedSetting = verifyUow.Query<ISetting>()
                    .FirstOrDefault(x => x.Name == testSettingName && x.ScopedKey == tenantCode);
                retrievedSetting.ShouldNotBeNull();
                retrievedSetting.Value.GetValue<string>().ShouldBe(testValue);
            }
        }
    }
}
