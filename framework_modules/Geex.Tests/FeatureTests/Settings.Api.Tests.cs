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
            var requestBody = """
                {
                    "query": "query { settings(request: { scope: Global }) { items { id name scope scopedKey value __typename } totalCount __typename } }"
                }
                """;

            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(GqlEndpoint, content);
            var (responseData, responseString) = await response.ParseGraphQLResponse();

            int totalCount = responseData["data"]["settings"]["totalCount"].GetValue<int>();
            totalCount.ShouldBeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public async Task QueryInitSettingsShouldWork()
        {
            // Arrange & Act & Assert - GraphQL queries don't need separate scopes
            var client = this.SuperAdminClient;
            var requestBody = """
                {
                    "query": "query { initSettings { id name scope scopedKey value __typename } }"
                }
                """;

            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(GqlEndpoint, content);
            var (responseData, responseString) = await response.ParseGraphQLResponse();

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
            var requestBody = $$"""
                {
                    "query": "mutation { editSetting(request: { scope: Global, scopedKey: null, name: {{testSettingName}}, value: \"{{testValue}}\" }) { id name scope scopedKey value __typename } }"
                }
                """;

            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(GqlEndpoint, content);
            var (responseData, responseString) = await response.ParseGraphQLResponse();

            ((string)responseData["data"]["editSetting"]["name"]).ShouldBe(testSettingName);
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
            var requestBody = $$"""
                {
                    "query": "query { settings(request: { scope: Global }, filter: { name: { eq: {{targetSettingName}} } }) { items { id name scope scopedKey value __typename } totalCount __typename } }"
                }
                """;

            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(GqlEndpoint, content);
            var (responseData, responseString) = await response.ParseGraphQLResponse();

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
            var testSettingName = TestModuleSettings.GlobalSetting;
            string testValue = ObjectId.GenerateNewId().ToString();

            // Act - Edit setting using GraphQL
            var client = this.SuperAdminClient;
            var editRequestBody = $$"""
                {
                    "query": "mutation { editSetting(request: { scope: Global, scopedKey: null, name: {{testSettingName}}, value: \"{{testValue}}\" }) { id name scope scopedKey value __typename } }"
                }
                """;

            var editContent = new StringContent(editRequestBody, Encoding.UTF8, "application/json");

            var editResponse = await client.PostAsync(GqlEndpoint, editContent);
            var (editResponseData, _) = await editResponse.ParseGraphQLResponse();

            // Assert - Edit successful
            ((string)editResponseData["data"]["editSetting"]["name"]).ShouldBe(testSettingName);
            ((string)editResponseData["data"]["editSetting"]["value"]).ShouldBe(testValue);

            // Act - Query to verify using GraphQL
            var queryRequestBody = $$"""
                {
                    "query": "query { settings(request: { scope: Global }, filter: { name: { eq: {{testSettingName}} } }) { items { id name scope scopedKey value __typename } totalCount __typename } }"
                }
                """;

            var queryContent = new StringContent(queryRequestBody, Encoding.UTF8, "application/json");

            var queryResponse = await client.PostAsync(GqlEndpoint, queryContent);
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
            var testSettingName = TestModuleSettings.GlobalSetting;
            var complexValue = """[{text:"<span class=\"nav-group-text\">系统及配置</span>",icon:null,shortcutRoot:false,link:null,badge:0,acl:["identity_query_orgs"],shortcut:false,i18n:null,group:true,hideInBreadcrumb:true}]""".Replace("\\","\\\\").Replace("\"","\\\"");

            // Act & Assert - GraphQL mutation
            var client = this.SuperAdminClient;
            var requestBody = $$"""
                {
                    "query": "mutation { editSetting(request: { scope: Global, scopedKey: null, name: {{testSettingName}}, value: {{complexValue}} }) { name value __typename } }"
                }
                """;

            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(GqlEndpoint, content);
            var (responseData, responseString) = await response.ParseGraphQLResponse();

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
            var requestBody = """
                {
                    "query": "query { settings(request: { scope: Global }, filter: { scope: { eq: Global } }) { items { id name scope scopedKey value } totalCount } }"
                }
                """;

            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(GqlEndpoint, content);
            var (responseData, responseString) = await response.ParseGraphQLResponse();

            var items = responseData["data"]["settings"]["items"].AsArray();
            items.Count.ShouldBeGreaterThan(0);

            foreach (var item in items)
            {
                ((string)item["scope"]).ShouldBe("Global");
            }
        }
    }
}
