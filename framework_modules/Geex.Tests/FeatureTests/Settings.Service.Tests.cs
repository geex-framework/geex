using System;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using Geex.Extensions.Settings;
using Geex.Extensions.Settings.Requests;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;

using Shouldly;

using Xunit;

namespace Geex.Tests.FeatureTests
{
    [Collection(nameof(TestsCollection))]
    public class SettingsServiceTests
    {
        private readonly TestApplicationFactory _factory;

        public SettingsServiceTests(TestApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task EditSettingServiceShouldWork()
        {
            // Arrange
            var service = _factory.Services;
            var uow = service.GetService<IUnitOfWork>();
            var testSettingName = TestModuleSettings.GlobalSetting;
            var testValue = ObjectId.GenerateNewId().ToString();

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
            setting.Value.ShouldBe(testValue);
            setting.Scope.ShouldBe(SettingScopeEnumeration.Global);
        }

        [Fact]
        public async Task GetSettingsServiceShouldWork()
        {
            // Arrange
            var service = _factory.Services;
            var uow = service.GetService<IUnitOfWork>();
            var testSettingName = TestModuleSettings.GlobalSetting;
            var testValue = ObjectId.GenerateNewId().ToString();

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
            using var service1 = service.CreateScope();
            var verifyUow = service1.ServiceProvider.GetService<IUnitOfWork>();
            var settings = verifyUow.Query<ISetting>().Where(x => x.Name == testSettingName).ToList();

            // Assert
            settings.ShouldNotBeEmpty();
            settings.First().Name.ShouldBe(testSettingName);
            settings.First().Value.ShouldBe(testValue);
        }

        [Fact]
        public async Task SettingWithDifferentScopesShouldWork()
        {
            // Arrange
            var service = _factory.Services;
            var uow = service.GetService<IUnitOfWork>();
            var testSettingName = TestModuleSettings.GlobalSetting;
            var globalValue = "GlobalValue_" + ObjectId.GenerateNewId();

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
            globalSetting.Value.ShouldBe(globalValue);

            // Verify retrieval
            using var service1 = service.CreateScope();
            var verifyUow = service1.ServiceProvider.GetService<IUnitOfWork>();
            var retrievedSetting = verifyUow.Query<ISetting>()
                .FirstOrDefault(x => x.Name == testSettingName && x.Scope == SettingScopeEnumeration.Global);
            retrievedSetting.ShouldNotBeNull();
            retrievedSetting.Value.ShouldBe(globalValue);
        }

        [Fact]
        public async Task ComplexSettingValueShouldWork()
        {
            // Arrange
            var service = _factory.Services;
            var uow = service.GetService<IUnitOfWork>();
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
            var service = _factory.Services;
            var uow = service.GetService<IUnitOfWork>();
            var testSettingName = TestModuleSettings.GlobalSetting;
            var originalValue = ObjectId.GenerateNewId().ToString();
            var updatedValue = ObjectId.GenerateNewId().ToString();

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
            updatedSetting.Value.ShouldBe(updatedValue);

            // Verify only one setting exists with this name
            using var verifyService = service.CreateScope();
            var verifyUow = verifyService.ServiceProvider.GetService<IUnitOfWork>();
            var allSettings = verifyUow.Query<ISetting>().Where(x => x.Name == testSettingName).ToList();
            allSettings.Count.ShouldBe(1);
            allSettings.First().Value.ShouldBe(updatedValue);
        }

        [Fact]
        public async Task SettingWithScopedKeyShouldWork()
        {
            // Arrange
            var service = _factory.Services;
            var uow = service.GetService<IUnitOfWork>();
            var testSettingName = TestModuleSettings.TenantSetting;
            var scopedKey = ObjectId.GenerateNewId().ToString();
            var testValue = ObjectId.GenerateNewId().ToString();

            // Act
            var setting = await uow.Request(new EditSettingRequest
            {
                Scope = SettingScopeEnumeration.Tenant,
                ScopedKey = scopedKey,
                Name = testSettingName,
                Value = testValue
            });
            await uow.SaveChanges();

            // Assert
            setting.ShouldNotBeNull();
            setting.Name.ShouldBe(testSettingName);
            setting.ScopedKey.ShouldBe(scopedKey);
            setting.Scope.ShouldBe(SettingScopeEnumeration.Tenant);
            setting.Value.ShouldBe(testValue);

            // Verify retrieval
            using var service1 = service.CreateScope();
            var verifyUow = service1.ServiceProvider.GetService<IUnitOfWork>();
            var retrievedSetting = verifyUow.Query<ISetting>()
                .FirstOrDefault(x => x.Name == testSettingName && x.ScopedKey == scopedKey);
            retrievedSetting.ShouldNotBeNull();
            retrievedSetting.Value.ShouldBe(testValue);
        }
    }
}
