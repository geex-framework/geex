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

        public static async Task<(JsonNode responseData, string responseString)> PostGqlRequest<T>(
            this HttpClient client,
            string endpoint,
            string query,
            T variables = default)
        {
            var request = new
            {
                query = query,
                variables = variables
            };

            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(endpoint, content);
            return await response.ParseGraphQLResponse();
        }

        public static async Task<(JsonNode responseData, string responseString)> PostGqlRequest(
            this HttpClient client,
            string endpoint,
            string query)
        {
            return await client.PostGqlRequest(endpoint, query, default(object));
        }
    }
}
