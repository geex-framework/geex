using System;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using Elastic.Apm.Api;

using Geex.Extensions.Settings;
using Geex.Extensions.Settings.Requests;

using Microsoft.Extensions.DependencyInjection;

using MongoDB.Bson;

using Newtonsoft.Json;

using Shouldly;

using Xunit;

namespace Geex.Tests.FeatureTests
{
    [Collection(nameof(TestsCollection))]
    public class SettingsApiTests : TestsBase
    {
        public SettingsApiTests(TestApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task QuerySettingsShouldWork()
        {
            // Arrange & Act & Assert - GraphQL queries don't need separate scopes
            var client = this.SuperAdminClient;
            var query = """
                query {
                    settings(request: { scope: Global }) {
                        items { id name scope scopedKey value __typename }
                        totalCount __typename
                    }
                }
                """;

            var (responseData, responseString) = await client.PostGqlRequest( query);

            int totalCount = responseData["data"]["settings"]["totalCount"].GetValue<int>();
            totalCount.ShouldBeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public async Task QueryInitSettingsShouldWork()
        {
            // Arrange & Act & Assert - GraphQL queries don't need separate scopes
            var client = this.SuperAdminClient;
            var query = """
                query {
                    initSettings {
                        id name scope scopedKey value __typename
                    }
                }
                """;

            var (responseData, responseString) = await client.PostGqlRequest( query);

            var initSettings = responseData["data"]["initSettings"].AsArray();
            int settingsCount = initSettings.Count;
            settingsCount.ShouldBeGreaterThan(0);
        }

        [Fact]
        public async Task EditSettingMutationShouldWork()
        {
            // Arrange
            var testSettingName = TestModuleSettings.GlobalSetting;
            var testValue = ObjectId.GenerateNewId().ToString();

            // Act & Assert - GraphQL mutations are handled by the framework's scope management
            var client = this.SuperAdminClient;
            var query = """
                mutation($name: SettingDefinition!, $value: Any!) {
                    editSetting(request: { scope: Global, scopedKey: null, name: $name, value: $value }) {
                        id name scope scopedKey value __typename
                    }
                }
                """;

            var (responseData, responseString) = await client.PostGqlRequest( query, new { name = testSettingName.Name, value = testValue });

            ((string)responseData["data"]["editSetting"]["name"]).ShouldBe(testSettingName.Name);
            ((string)responseData["data"]["editSetting"]["value"]).ShouldBe(testValue);
        }

        [Fact]
        public async Task FilterSettingsByNameShouldWork()
        {
            // Arrange - Prepare data using separate scope
            var targetSettingName = TestModuleSettings.GlobalSetting;
            var testValue = ObjectId.GenerateNewId().ToString();

            using (var scope = ScopedService.CreateScope())
            {
                var setupUow = scope.ServiceProvider.GetService<IUnitOfWork>();
                await setupUow.Request(new EditSettingRequest
                {
                    Scope = SettingScopeEnumeration.Global,
                    ScopedKey = null,
                    Name = targetSettingName,
                    Value = testValue
                });
                await setupUow.SaveChanges();
            }

            // Act & Assert - Query using GraphQL
            var client = this.SuperAdminClient;
            var query = """
                query($name: SettingDefinition!) {
                    settings(request: { scope: Global }, filter: { name: { eq: $name } }) {
                        items { id name scope scopedKey value __typename }
                        totalCount __typename
                    }
                }
                """;

            var (responseData, responseString) = await client.PostGqlRequest( query, new { name = targetSettingName.Name });

            var items = responseData["data"]["settings"]["items"].AsArray();
            items.Count.ShouldBeGreaterThan(0);

            foreach (var item in items)
            {
                ((string)item["name"]).ShouldBe(targetSettingName.Name);
            }
        }

        [Fact]
        public async Task EditAndVerifySettingShouldWork()
        {
            // Arrange
            var testSettingName = TestModuleSettings.GlobalSetting;
            string testValue = ObjectId.GenerateNewId().ToString();

            // Act - Edit setting using GraphQL
            var client = this.SuperAdminClient;
            var editQuery = """
                mutation($name: SettingDefinition!, $value: Any!) {
                    editSetting(request: { scope: Global, scopedKey: null, name: $name, value: $value }) {
                        id name scope scopedKey value __typename
                    }
                }
                """;

            var (editResponseData, _) = await client.PostGqlRequest( editQuery, new { name = testSettingName.Name, value = testValue });

            // Assert - Edit successful
            ((string)editResponseData["data"]["editSetting"]["name"]).ShouldBe(testSettingName.Name);
            ((string)editResponseData["data"]["editSetting"]["value"]).ShouldBe(testValue);

            // Act - Query to verify using GraphQL
            var queryQuery = """
                query testQuery($name: SettingDefinition!) {
                    settings(request: { scope: Global }, filter: { name: { eq: $name } }) {
                        items { id name scope scopedKey value __typename }
                        totalCount __typename
                    }
                }
                """;

            var (queryResponseData, responseString) = await client.PostGqlRequest( queryQuery, new { name = testSettingName.Name });

            // Assert - Query successful and value updated
            var items = queryResponseData["data"]["settings"]["items"].AsArray();
            items.Count.ShouldBe(1);

            var item = items[0];
            ((string)item["name"]).ShouldBe(testSettingName.Name);
            ((string)item["value"]).ShouldBe(testValue);
        }

        [Fact]
        public async Task ComplexSettingValueMutationShouldWork()
        {
            // Arrange
            var testSettingName = TestModuleSettings.GlobalSetting;
            object[] complexValue = [new { text = "<span class=\"nav-group-text\">系统及配置</span>", icon = default(object), shortcutRoot = false, link = default(object), badge = 0, acl = (string[])["identity_query_orgs"], shortcut = false, i18n = default(object), group = true, hideInBreadcrumb = true }];
            // Act & Assert - GraphQL mutation
            var client = this.SuperAdminClient;
            var query = """
                mutation($name: SettingDefinition!, $value: Any!) {
                    editSetting(request: { scope: Global, scopedKey: null, name: $name, value: $value }) {
                        name value __typename
                    }
                }
                """;

            var (responseData, responseString) = await client.PostGqlRequest( query, new { name = testSettingName.Name, value = complexValue });

            ((string)responseData["data"]["editSetting"]["name"]).ShouldBe(testSettingName);
        }

        [Fact]
        public async Task FilterSettingsByScopeShouldWork()
        {
            // Arrange - Prepare data using separate scope
            var testSettingName = TestModuleSettings.GlobalSetting;
            var testValue = ObjectId.GenerateNewId().ToString();

            using (var scope = ScopedService.CreateScope())
            {
                var setupUow = scope.ServiceProvider.GetService<IUnitOfWork>();
                await setupUow.Request(new EditSettingRequest
                {
                    Scope = SettingScopeEnumeration.Global,
                    ScopedKey = null,
                    Name = testSettingName,
                    Value = testValue
                });
                await setupUow.SaveChanges();
            }

            // Act & Assert - Query using GraphQL
            var client = this.SuperAdminClient;
            var query = """
                query {
                    settings(request: { scope: Global }, filter: { scope: { eq: Global } }) {
                        items { id name scope scopedKey value }
                        totalCount
                    }
                }
                """;

            var (responseData, responseString) = await client.PostGqlRequest( query);

            var items = responseData["data"]["settings"]["items"].AsArray();
            items.Count.ShouldBeGreaterThan(0);

            foreach (var item in items)
            {
                ((string)item["scope"]).ShouldBe("Global");
            }
        }
    }
}
