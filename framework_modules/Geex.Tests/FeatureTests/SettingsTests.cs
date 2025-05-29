using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

using Newtonsoft.Json;

using Shouldly;

using Xunit;
namespace Geex.Tests.FeatureTests
{
    [Collection(nameof(TestsCollection))]
    public class SettingsTests
    {
        private readonly TestApplicationFactory _factory;
        private readonly string _graphqlEndpoint = "/graphql";

        public SettingsTests(TestApplicationFactory factory)
        {
            _factory = factory;
        }

        /// <summary>
        /// 解析GraphQL响应并返回dynamic对象，方便访问结果
        /// </summary>
        private dynamic ParseGraphQLResponse(string responseContent)
        {
            return JsonConvert.DeserializeObject<dynamic>(responseContent);
        }
        [Fact]
        public async Task DynamicSettingMutationShouldWork()
        {
            // Arrange
            var client = _factory.CreateClient();
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
            };            var request = new
            {
                operationName = "editSetting",
                variables = new
                {
                    request = new
                    {
                        scope = "Global",
                        scopedKey = (string)null,
                        name = "BlobStorageModuleName",
                        value = new[] { navItem }
                    }
                },
                query = "mutation editSetting($request: EditSettingRequest!) {  editSetting(request: $request) {    name    value    __typename  }}"
            };

            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync(_graphqlEndpoint, content);
            var responseString = await response.Content.ReadAsStringAsync();

            // Parse response as dynamic
            dynamic responseData = ParseGraphQLResponse(responseString);

            // Assert
            ((string)responseData.data.editSetting.name).ShouldBe("BlobStorageModuleName");
            response.EnsureSuccessStatusCode();
        }
        [Fact]
        public async Task QuerySettingsShouldWork()
        {
            // Arrange
            var client = _factory.CreateClient();            var request = new
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
            var responseString = await response.Content.ReadAsStringAsync();

            // Parse response as dynamic
            dynamic responseData = ParseGraphQLResponse(responseString);

            // Assert
            int itemCount = ((Newtonsoft.Json.Linq.JArray)responseData.data.settings.items).Count;
            int totalCount = (int)responseData.data.settings.totalCount;

            itemCount.ShouldBeGreaterThan(0);
            totalCount.ShouldBeGreaterThan(0);

            response.EnsureSuccessStatusCode();
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
            var responseString = await response.Content.ReadAsStringAsync();

            // Parse response as dynamic
            dynamic responseData = ParseGraphQLResponse(responseString);

            // Assert
            int settingsCount = ((Newtonsoft.Json.Linq.JArray)responseData.data.initSettings).Count;
            settingsCount.ShouldBeGreaterThan(0);

            response.EnsureSuccessStatusCode();
        }
        [Fact]
        public async Task FilterSettingsByNameShouldWork()
        {
            // Arrange
            var client = _factory.CreateClient();
            var targetSettingName = "BlobStorageModuleName";            var request = new
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
            var responseString = await response.Content.ReadAsStringAsync();

            // Parse response as dynamic
            dynamic responseData = ParseGraphQLResponse(responseString);

            // Assert
            var items = responseData.data.settings.items;
            ((Newtonsoft.Json.Linq.JArray)items).Count.ShouldBeGreaterThan(0);

            foreach (var item in items)
            {
                ((string)item.name).ShouldBe(targetSettingName);
            }

            response.EnsureSuccessStatusCode();
        }
        [Fact]
        public async Task EditAndVerifySettingShouldWork()
        {
            // Arrange
            var client = _factory.CreateClient();
            string testSettingName = "BlobStorageModuleName";
            string testValue = ObjectId.GenerateNewId().ToString();            // 1. 编辑设置
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

            // Act - 编辑设置
            var editResponse = await client.PostAsync(_graphqlEndpoint, editContent);
            var editResponseString = await editResponse.Content.ReadAsStringAsync();

            // Parse response as dynamic
            dynamic editResponseData = ParseGraphQLResponse(editResponseString);

            // Assert - 编辑成功
            ((string)editResponseData.data.editSetting.name).ShouldBe(testSettingName);
            ((string)editResponseData.data.editSetting.value).ShouldBe(testValue);
            editResponse.EnsureSuccessStatusCode();            // 2. 查询设置以验证
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

            // Act - 查询设置
            var queryResponse = await client.PostAsync(_graphqlEndpoint, queryContent);
            var queryResponseString = await queryResponse.Content.ReadAsStringAsync();

            // Parse response as dynamic
            dynamic queryResponseData = ParseGraphQLResponse(queryResponseString);

            // Assert - 查询成功并且值已更新
            var items = queryResponseData.data.settings.items;
            ((Newtonsoft.Json.Linq.JArray)items).Count.ShouldBe(1);

            var item = items[0];
            ((string)item.name).ShouldBe(testSettingName);
            ((string)item.value).ShouldBe(testValue);

            queryResponse.EnsureSuccessStatusCode();
        }
        [Fact]
        public async Task EditSettingWithDifferentScopesShouldWork()
        {
            // Arrange
            var client = _factory.CreateClient();
            string testSettingName = "BlobStorageModuleName";
            string globalValue = "GlobalValue_" + ObjectId.GenerateNewId();            // 1. 编辑全局设置
            var editGlobalRequest = new
            {
                operationName = "editSetting",
                variables = new
                {
                    request = new
                    {
                        scope = "Global",
                        scopedKey = (string)null,
                        name = testSettingName,
                        value = globalValue
                    }
                },
                query = "mutation editSetting($request: EditSettingRequest!) {  editSetting(request: $request) {    id    name    scope    scopedKey    value    __typename  }}"
            };

            var editGlobalContent = new StringContent(JsonConvert.SerializeObject(editGlobalRequest), Encoding.UTF8, "application/json");

            // Act - 编辑全局设置
            var editGlobalResponse = await client.PostAsync(_graphqlEndpoint, editGlobalContent);
            var editGlobalResponseString = await editGlobalResponse.Content.ReadAsStringAsync();

            // Parse response as dynamic
            dynamic editGlobalResponseData = ParseGraphQLResponse(editGlobalResponseString);

            // Assert - 编辑全局设置成功
            var editSetting = editGlobalResponseData.data.editSetting;
            ((string)editSetting.name).ShouldBe(testSettingName);
            ((string)editSetting.scope).ShouldBe("Global");
            ((string)editSetting.value).ShouldBe(globalValue);
            editGlobalResponse.EnsureSuccessStatusCode();            // 2. 查询全局设置以验证
            var queryGlobalRequest = new
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

            var queryGlobalContent = new StringContent(JsonConvert.SerializeObject(queryGlobalRequest), Encoding.UTF8, "application/json");

            // Act - 查询全局设置
            var queryGlobalResponse = await client.PostAsync(_graphqlEndpoint, queryGlobalContent);
            var queryGlobalResponseString = await queryGlobalResponse.Content.ReadAsStringAsync();

            // Parse response as dynamic
            dynamic queryGlobalResponseData = ParseGraphQLResponse(queryGlobalResponseString);

            // Assert - 查询全局设置成功
            var items = queryGlobalResponseData.data.settings.items;
            ((Newtonsoft.Json.Linq.JArray)items).Count.ShouldBe(1);

            var item = items[0];
            ((string)item.name).ShouldBe(testSettingName);
            ((string)item.scope).ShouldBe("Global");
            ((string)item.value).ShouldBe(globalValue);

            queryGlobalResponse.EnsureSuccessStatusCode();
        }
    }
}
