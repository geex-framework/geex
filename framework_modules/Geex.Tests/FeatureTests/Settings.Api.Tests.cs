using System;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

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
    public class SettingsApiTests
    {
        private readonly TestApplicationFactory _factory;
        private readonly string _graphqlEndpoint = "/graphql";

        public SettingsApiTests(TestApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task QuerySettingsShouldWork()
        {
            // Arrange
            var client = _factory.CreateClient();
            var request = new
            {
                operationName = "settings",
                variables = new
                {
                    request = new
                    {
                        scope = "Global"
                    }
                },
                query = "query settings($request: GetSettingsRequest!) {  settings(request: $request) {    items {      id      name      scope      scopedKey      value      __typename    }    totalCount    __typename  }}"
            };

            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync(_graphqlEndpoint, content);

            var (responseData, responseString) = await response.ParseGraphQLResponse();

            // Assert
            int totalCount = responseData["data"]["settings"]["totalCount"].GetValue<int>();
            totalCount.ShouldBeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public async Task QueryInitSettingsShouldWork()
        {
            // Arrange
            var client = _factory.CreateClient();
            var request = new
            {
                operationName = "initSettings",
                variables = new { },
                query = "query initSettings {  initSettings {    id    name    scope    scopedKey    value    __typename  }}"
            };

            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync(_graphqlEndpoint, content);

            var (responseData, responseString) = await response.ParseGraphQLResponse();

            // Assert
            var initSettings = responseData["data"]["initSettings"].AsArray();
            int settingsCount = initSettings.Count;
            settingsCount.ShouldBeGreaterThan(0);
        }

        [Fact]
        public async Task EditSettingMutationShouldWork()
        {
            // Arrange
            var client = _factory.CreateClient();
            var testSettingName = $"ApiEditTest_{ObjectId.GenerateNewId()}";
            var testValue = ObjectId.GenerateNewId().ToString();

            var request = new
            {
                operationName = "editSetting",
                variables = new
                {
                    request = new
                    {
                        scope = "Global",
                        scopedKey = (string)null,
                        name = testSettingName,
                        value = testValue
                    }
                },
                query = "mutation editSetting($request: EditSettingRequest!) {  editSetting(request: $request) {    id    name    scope    scopedKey    value    __typename  }}"
            };

            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync(_graphqlEndpoint, content);

            var (responseData, responseString) = await response.ParseGraphQLResponse();

            // Assert
            ((string)responseData["data"]["editSetting"]["name"]).ShouldBe(testSettingName);
            ((string)responseData["data"]["editSetting"]["value"]).ShouldBe(testValue);
        }

        [Fact]
        public async Task FilterSettingsByNameShouldWork()
        {
            // Arrange
            var client = _factory.CreateClient();
            var targetSettingName = TestModuleSettings.GlobalSetting;
            var testValue = ObjectId.GenerateNewId().ToString();

            // Prepare data using separate scope
            using (var scope = _factory.Services.CreateScope())
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

            var request = new
            {
                operationName = "settings",
                variables = new
                {
                    request = new
                    {
                        scope = "Global"
                    },
                    filter = new
                    {
                        name = new
                        {
                            eq = targetSettingName
                        }
                    }
                },
                query = "query settings($request: GetSettingsRequest!, $filter: ISettingFilterInput) {  settings(request: $request, filter: $filter) {    items {      id      name      scope      scopedKey      value      __typename    }    totalCount    __typename  }}"
            };

            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync(_graphqlEndpoint, content);

            var (responseData, responseString) = await response.ParseGraphQLResponse();

            // Assert
            var items = responseData["data"]["settings"]["items"].AsArray();
            items.Count.ShouldBeGreaterThan(0);

            foreach (var item in items)
            {
                ((string)item["name"]).ShouldBe(targetSettingName);
            }
        }

        [Fact]
        public async Task EditAndVerifySettingShouldWork()
        {
            // Arrange
            var client = _factory.CreateClient();
            var testSettingName = $"EditVerifyTest_{ObjectId.GenerateNewId()}";
            string testValue = ObjectId.GenerateNewId().ToString();

            // 1. Edit setting
            var editRequest = new
            {
                operationName = "editSetting",
                variables = new
                {
                    request = new
                    {
                        scope = "Global",
                        scopedKey = (string)null,
                        name = testSettingName,
                        value = testValue
                    }
                },
                query = "mutation editSetting($request: EditSettingRequest!) {  editSetting(request: $request) {    id    name    scope    scopedKey    value    __typename  }}"
            };

            var editContent = new StringContent(JsonConvert.SerializeObject(editRequest), Encoding.UTF8, "application/json");

            // Act - Edit setting
            var editResponse = await client.PostAsync(_graphqlEndpoint, editContent);

            var (editResponseData, _) = await editResponse.ParseGraphQLResponse();

            // Assert - Edit successful
            ((string)editResponseData["data"]["editSetting"]["name"]).ShouldBe(testSettingName);
            ((string)editResponseData["data"]["editSetting"]["value"]).ShouldBe(testValue);

            // 2. Query to verify
            var queryRequest = new
            {
                operationName = "settings",
                variables = new
                {
                    request = new
                    {
                        scope = "Global"
                    },
                    filter = new
                    {
                        name = new
                        {
                            eq = testSettingName
                        }
                    }
                },
                query = "query settings($request: GetSettingsRequest!, $filter: ISettingFilterInput) {  settings(request: $request, filter: $filter) {    items {      id      name      scope      scopedKey      value      __typename    }    totalCount    __typename  }}"
            };

            var queryContent = new StringContent(JsonConvert.SerializeObject(queryRequest), Encoding.UTF8, "application/json");

            // Act - Query setting
            var queryResponse = await client.PostAsync(_graphqlEndpoint, queryContent);

            var (queryResponseData, responseString) = await queryResponse.ParseGraphQLResponse();

            // Assert - Query successful and value updated
            var items = queryResponseData["data"]["settings"]["items"].AsArray();
            items.Count.ShouldBe(1);

            var item = items[0];
            ((string)item["name"]).ShouldBe(testSettingName);
            ((string)item["value"]).ShouldBe(testValue);
        }

        [Fact]
        public async Task ComplexSettingValueMutationShouldWork()
        {
            // Arrange
            var client = _factory.CreateClient();
            var testSettingName = $"ComplexValueTest_{ObjectId.GenerateNewId()}";
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

            var request = new
            {
                operationName = "editSetting",
                variables = new
                {
                    request = new
                    {
                        scope = "Global",
                        scopedKey = (string)null,
                        name = testSettingName,
                        value = new[] { navItem }
                    }
                },
                query = "mutation editSetting($request: EditSettingRequest!) {  editSetting(request: $request) {    name    value    __typename  }}"
            };

            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync(_graphqlEndpoint, content);

            var (responseData, responseString) = await response.ParseGraphQLResponse();

            // Assert
            ((string)responseData["data"]["editSetting"]["name"]).ShouldBe(testSettingName);
        }

        [Fact]
        public async Task FilterSettingsByScopeShouldWork()
        {
            // Arrange
            var client = _factory.CreateClient();
            var testSettingName = TestModuleSettings.GlobalSetting;
            var testValue = ObjectId.GenerateNewId().ToString();

            // Prepare data using separate scope
            using (var scope = _factory.Services.CreateScope())
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

            var request = new
            {
                query = $$"""
                    query {
                        settings(request: { scope: Global }, filter: { scope: { eq: Global } }) {
                            items {
                                id
                                name
                                scope
                                scopedKey
                                value
                            }
                            totalCount
                        }
                    }
                    """
            };

            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync(_graphqlEndpoint, content);

            var (responseData, responseString) = await response.ParseGraphQLResponse();

            // Assert
            var items = responseData["data"]["settings"]["items"].AsArray();
            items.Count.ShouldBeGreaterThan(0);

            foreach (var item in items)
            {
                ((string)item["scope"]).ShouldBe("Global");
            }
        }
    }
}
