using System;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using Geex.Extensions.ApprovalFlows.Core.Entities;
using Geex.Extensions.MultiTenant.Core.Aggregates.Tenants;
using Geex.Extensions.Requests.MultiTenant;
using Geex.Extensions.Settings;
using Geex.Extensions.Settings.Requests;
using Geex.MultiTenant;

using Microsoft.Extensions.DependencyInjection;

using MongoDB.Bson;

using Shouldly;

using Xunit;

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
            var uow = ScopedService.GetService<IUnitOfWork>();
            var testSettingName = TestModuleSettings.GlobalSetting;
            var testValue = ObjectId.GenerateNewId().ToString();

            // Ensure we're in host context for global settings
            var currentTenant = uow.ServiceProvider.GetService<ICurrentTenant>();
            currentTenant.Change(null);

            // Act
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
        [Fact]
        public async Task GetSettingsServiceShouldWork()
        {
            // Arrange
            var uow = ScopedService.GetService<IUnitOfWork>();
            var testSettingName = TestModuleSettings.GlobalSetting;
            var testValue = ObjectId.GenerateNewId().ToString();

            // Ensure we're in host context for global settings
            var currentTenant = uow.ServiceProvider.GetService<ICurrentTenant>();
            currentTenant.Change(null);

            // Create test setting
            await uow.Request(new EditSettingRequest
            {
                Scope = SettingScopeEnumeration.Global,
                ScopedKey = null,
                Name = testSettingName,
                Value = testValue
            });
            await uow.SaveChanges();

            // Act
            using var service1 = ScopedService.CreateScope();
            var verifyUow = service1.ServiceProvider.GetService<IUnitOfWork>();
            var settings = verifyUow.Query<ISetting>().Where(x => x.Name == testSettingName).ToList();

            // Assert
            settings.ShouldNotBeEmpty();
            settings.First().Name.ShouldBe(testSettingName);
            settings.First().Value.GetValue<string>().ShouldBe(testValue);
        }
        [Fact]
        public async Task SettingWithDifferentScopesShouldWork()
        {
            // Arrange
            var uow = ScopedService.GetService<IUnitOfWork>();
            var testSettingName = TestModuleSettings.GlobalSetting;
            var globalValue = "GlobalValue_" + ObjectId.GenerateNewId();

            // Ensure we're in host context for global settings
            var currentTenant = uow.ServiceProvider.GetService<ICurrentTenant>();
            currentTenant.Change(null);

            // Act - Create global setting
            var globalSetting = await uow.Request(new EditSettingRequest
            {
                Scope = SettingScopeEnumeration.Global,
                ScopedKey = null,
                Name = testSettingName,
                Value = globalValue
            });
            await uow.SaveChanges();

            // Assert
            globalSetting.ShouldNotBeNull();
            globalSetting.Scope.ShouldBe(SettingScopeEnumeration.Global);
            globalSetting.Value.GetValue<string>().ShouldBe(globalValue);

            // Verify retrieval
            using var service1 = ScopedService.CreateScope();
            var verifyUow = service1.ServiceProvider.GetService<IUnitOfWork>();
            var retrievedSetting = verifyUow.Query<ISetting>()
                .FirstOrDefault(x => x.Name == testSettingName && x.Scope == SettingScopeEnumeration.Global);
            retrievedSetting.ShouldNotBeNull();
            retrievedSetting.Value.GetValue<string>().ShouldBe(globalValue);
        }
        [Fact]
        public async Task ComplexSettingValueShouldWork()
        {
            // Arrange
            var uow = ScopedService.GetService<IUnitOfWork>();
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

            // Ensure we're in host context for global settings
            var currentTenant = uow.ServiceProvider.GetService<ICurrentTenant>();
            currentTenant.Change(null);

            // Act
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
        [Fact]
        public async Task UpdateExistingSettingServiceShouldWork()
        {
            // Arrange
            var uow = ScopedService.GetService<IUnitOfWork>();
            var testSettingName = TestModuleSettings.GlobalSetting;
            var originalValue = ObjectId.GenerateNewId().ToString();
            var updatedValue = ObjectId.GenerateNewId().ToString();

            // Ensure we're in host context for global settings
            var currentTenant = uow.ServiceProvider.GetService<ICurrentTenant>();
            currentTenant.Change(null);

            // Create initial setting
            await uow.Request(new EditSettingRequest
            {
                Scope = SettingScopeEnumeration.Global,
                ScopedKey = null,
                Name = testSettingName,
                Value = originalValue
            });
            await uow.SaveChanges();

            // Act - Update the setting
            var updatedSetting = await uow.Request(new EditSettingRequest
            {
                Scope = SettingScopeEnumeration.Global,
                ScopedKey = null,
                Name = testSettingName,
                Value = updatedValue
            });
            await uow.SaveChanges();

            // Assert
            updatedSetting.ShouldNotBeNull();
            updatedSetting.Name.ShouldBe(testSettingName);
            updatedSetting.Value.GetValue<string>().ShouldBe(updatedValue);

            // Verify only one setting exists with this name
            using var verifyService = ScopedService.CreateScope();
            var verifyUow = verifyService.ServiceProvider.GetService<IUnitOfWork>();
            var allSettings = verifyUow.Query<ISetting>().Where(x => x.Name == testSettingName).ToList();
            allSettings.Count.ShouldBe(1);
            allSettings.First().Value.GetValue<string>().ShouldBe(updatedValue);
        }

        [Fact]
        public async Task SettingWithScopedKeyShouldWork()
        {
            // Arrange
            var uow = ScopedService.GetService<IUnitOfWork>();
            var testSettingName = TestModuleSettings.TenantSetting;
            var tenantCode = "test";
            var testValue = ObjectId.GenerateNewId().ToString();

            var newTenant = uow.Create(new CreateTenantRequest()
            {
                Code = tenantCode,
                ExternalInfo = null,
                Name = "testName"
            });

            var currentTenant = uow.ServiceProvider.GetService<ICurrentTenant>();
            currentTenant.Change(tenantCode);

            // Act
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

            // Verify retrieval
            using var service1 = ScopedService.CreateScope();
            var verifyUow = service1.ServiceProvider.GetService<IUnitOfWork>();
            var retrievedSetting = verifyUow.Query<ISetting>()
                .FirstOrDefault(x => x.Name == testSettingName && x.ScopedKey == tenantCode);
            retrievedSetting.ShouldNotBeNull();
            retrievedSetting.Value.GetValue<string>().ShouldBe(testValue);
        }
    }
}
