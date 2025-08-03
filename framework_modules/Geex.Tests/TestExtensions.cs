using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;


namespace Geex.Tests
{
    internal static class TestExtensions
    {
        /// <summary>
        /// 解析GraphQL响应并返回dynamic对象，方便访问结果
        /// </summary>
        public static async Task<(JsonNode, string)> ParseGraphQLResponse(this HttpResponseMessage response, bool ignoreError = false)
        {
            var responseString = await response.Content.ReadAsStringAsync();
            JsonNode? result = JsonNode.Parse(responseString);
            if (!ignoreError)
            {
                try
                {
                    response.EnsureSuccessStatusCode();
                }
                catch (Exception e)
                {
                    var errorNode = result["errors"];
                    if (errorNode != default)
                    {
                        throw new Exception(errorNode.ToJsonString(), e);
                    }
                    throw;
                }
            }
            return (result, responseString);
        }

        public static async Task<(JsonNode responseData, string responseString)> PostGqlRequest<T>(
            this HttpClient client,
            string endpoint,
            string query,
            T variables = default,
            bool ignoreError = false
            )
        {
            var request = new
            {
                query = query,
                variables = variables
            };

            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(endpoint, content);
            return await response.ParseGraphQLResponse(ignoreError);
        }

        public static async Task<(JsonNode responseData, string responseString)> PostGqlRequest<T>(
            this HttpClient client,
            string query,
            T variables = default,
            bool ignoreError = false)
        {
            return await client.PostGqlRequest<T>("/graphql", query, variables, ignoreError);
        }

        public static async Task<(JsonNode responseData, string responseString)> PostGqlRequest(
            this HttpClient client,
            string endpoint,
            string query,
            bool ignoreError = false)
        {
            return await client.PostGqlRequest(endpoint, query, default(object), ignoreError);
        }

        public static async Task<(JsonNode responseData, string responseString)> PostGqlRequest(
            this HttpClient client,
            string query,
            bool ignoreError = false)
        {
            return await client.PostGqlRequest("/graphql", query, default(object), ignoreError);
        }
    }
}
