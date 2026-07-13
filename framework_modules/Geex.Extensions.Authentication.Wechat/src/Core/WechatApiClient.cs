using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Geex.Extensions.Authentication.Wechat.Core
{
    public class WechatApiClient : IWechatApiClient
    {
        private readonly HttpClient _httpClient;

        public WechatApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress ??= new Uri("https://api.weixin.qq.com/");
        }

        public async Task<WechatSessionResult> ExchangeWebCodeAsync(string appId, string appSecret, string code, CancellationToken cancellationToken = default)
        {
            var url = $"sns/oauth2/access_token?appid={Uri.EscapeDataString(appId)}&secret={Uri.EscapeDataString(appSecret)}&code={Uri.EscapeDataString(code)}&grant_type=authorization_code";
            var response = await _httpClient.GetFromJsonAsync<WechatApiResponse>(url, cancellationToken);
            return Map(response);
        }

        public async Task<WechatSessionResult> ExchangeMiniProgramCodeAsync(string appId, string appSecret, string code, CancellationToken cancellationToken = default)
        {
            var url = $"sns/jscode2session?appid={Uri.EscapeDataString(appId)}&secret={Uri.EscapeDataString(appSecret)}&js_code={Uri.EscapeDataString(code)}&grant_type=authorization_code";
            var response = await _httpClient.GetFromJsonAsync<WechatApiResponse>(url, cancellationToken);
            return Map(response);
        }

        public async Task<Dictionary<string, string>?> GetWebUserInfoAsync(string accessToken, string openId, CancellationToken cancellationToken = default)
        {
            var url = $"sns/userinfo?access_token={Uri.EscapeDataString(accessToken)}&openid={Uri.EscapeDataString(openId)}";
            var response = await _httpClient.GetFromJsonAsync<WechatUserInfoResponse>(url, cancellationToken);
            if (response == null || (!string.IsNullOrEmpty(response.ErrCode) && response.ErrCode != "0"))
            {
                return null;
            }

            var claims = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(response.Nickname))
            {
                claims["nickname"] = response.Nickname;
            }
            if (!string.IsNullOrEmpty(response.HeadImgUrl))
            {
                claims["avatar"] = response.HeadImgUrl;
            }
            if (!string.IsNullOrEmpty(response.UnionId))
            {
                claims["unionid"] = response.UnionId;
            }
            return claims;
        }

        private static WechatSessionResult Map(WechatApiResponse? response)
        {
            response ??= new WechatApiResponse();
            return new WechatSessionResult
            {
                OpenId = response.OpenId,
                UnionId = response.UnionId,
                AccessToken = response.AccessToken,
                ErrCode = response.ErrCode?.ToString(),
                ErrMsg = response.ErrMsg,
            };
        }

        private class WechatApiResponse
        {
            [JsonPropertyName("openid")]
            public string? OpenId { get; set; }
            [JsonPropertyName("unionid")]
            public string? UnionId { get; set; }
            [JsonPropertyName("access_token")]
            public string? AccessToken { get; set; }
            [JsonPropertyName("errcode")]
            public int? ErrCode { get; set; }
            [JsonPropertyName("errmsg")]
            public string? ErrMsg { get; set; }
        }

        private class WechatUserInfoResponse
        {
            [JsonPropertyName("nickname")]
            public string? Nickname { get; set; }
            [JsonPropertyName("headimgurl")]
            public string? HeadImgUrl { get; set; }
            [JsonPropertyName("unionid")]
            public string? UnionId { get; set; }
            [JsonPropertyName("errcode")]
            public string? ErrCode { get; set; }
        }
    }
}
