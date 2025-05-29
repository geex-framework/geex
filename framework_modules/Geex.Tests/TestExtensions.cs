using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace Geex.Tests
{
    internal static class TestExtensions
    {
        /// <summary>
        /// 解析GraphQL响应并返回dynamic对象，方便访问结果
        /// </summary>
        public static async Task<dynamic> ParseGraphQLResponse(this HttpResponseMessage response)
        {
            var responseString = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<dynamic>(responseString);
            return result;
        }
    }
}
