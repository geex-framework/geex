using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using Geex.Common;
using Geex.Common.Abstraction.Approval;
using Geex.Common.Abstraction.Gql.Types;
using Geex.Common.Requests.Settings;
using Geex.Common.Settings;
using Geex.Common.Settings.Abstraction;
using Geex.Tests.TestEntities;

using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Geex.Tests
{
    public class SettingsTests : IClassFixture<GeexWebApplicationFactory>
    {
        private readonly GeexWebApplicationFactory _factory;

        public SettingsTests(GeexWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task DynamicSettingMutationShouldWork()
        {
            // Arrange
            var service = _factory.Services;

            var client = _factory.CreateClient();
            var url = "/graphql"; // 替换为你实际的 API 端点
            var content = new StringContent(
                """
                {
                  "operationName": "editSetting",
                  "variables": {
                    "input": {
                      "scope": "Global",
                      "scopedKey": null,
                      "name": "BlobStorageModuleName",
                      "value": [
                        {
                          "text": "<span class=\"nav-group-text\">系统及配置</span>",
                          "icon": null,
                          "shortcutRoot": false,
                          "link": null,
                          "badge": 0,
                          "acl": [
                            "identity_query_orgs"
                          ],
                          "shortcut": false,
                          "i18n": null,
                          "group": true,
                          "hideInBreadcrumb": true
                        }
                      ]
                    }
                  },
                  "query": "mutation editSetting($input: EditSettingRequest!) {  editSetting(request: $input) {    name    value    __typename  }}"
                }
                """
                , System.Text.Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync(url, content);

            // Assert
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            responseString.ShouldStartWith("{\"data\":{\"editSetting\":{\"name\":\"BlobStorageModuleName\",\"value\":[{");
        }
    }
}
