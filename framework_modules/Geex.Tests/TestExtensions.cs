using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;


namespace Geex.Tests
{
    internal static class TestExtensions
    {
        /// <summary>
        /// 解析GraphQL响应并返回dynamic对象，方便访问结果
        /// </summary>
        public static async Task<(JsonNode, string)> ParseGraphQLResponse(this HttpResponseMessage response)
        {
            var responseString = await response.Content.ReadAsStringAsync();
            var result = JsonNode.Parse(responseString);
            return (result, responseString);
        }
    }
}
