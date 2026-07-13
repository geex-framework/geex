using System.Collections.Concurrent;
using Geex.Extensions.Authentication.Wechat.Core;

namespace Geex.Tests.FeatureTests
{
    public class FakeWechatApiClient : IWechatApiClient
    {
        public static ConcurrentDictionary<string, WechatSessionResult> WebCodes { get; } = new();
        public static ConcurrentDictionary<string, WechatSessionResult> MiniProgramCodes { get; } = new();

        public Task<WechatSessionResult> ExchangeWebCodeAsync(string appId, string appSecret, string code, CancellationToken cancellationToken = default)
        {
            if (WebCodes.TryGetValue(code, out var result))
            {
                return Task.FromResult(result);
            }

            return Task.FromResult(new WechatSessionResult { ErrCode = "40029", ErrMsg = "invalid code" });
        }

        public Task<WechatSessionResult> ExchangeMiniProgramCodeAsync(string appId, string appSecret, string code, CancellationToken cancellationToken = default)
        {
            if (MiniProgramCodes.TryGetValue(code, out var result))
            {
                return Task.FromResult(result);
            }

            return Task.FromResult(new WechatSessionResult { ErrCode = "40029", ErrMsg = "invalid code" });
        }

        public Task<Dictionary<string, string>?> GetWebUserInfoAsync(string accessToken, string openId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Dictionary<string, string>?>(new Dictionary<string, string>
            {
                ["nickname"] = $"wx_{openId}",
            });
        }

        public static string RegisterWeb(string openId, string? unionId = null)
        {
            var code = global::MongoDB.Bson.ObjectId.GenerateNewId().ToString();
            WebCodes[code] = new WechatSessionResult
            {
                OpenId = openId,
                UnionId = unionId,
                AccessToken = $"access_{code}",
                ErrCode = "0",
            };
            return code;
        }

        public static string RegisterMiniProgram(string openId, string? unionId = null)
        {
            var code = global::MongoDB.Bson.ObjectId.GenerateNewId().ToString();
            MiniProgramCodes[code] = new WechatSessionResult
            {
                OpenId = openId,
                UnionId = unionId,
                ErrCode = "0",
            };
            return code;
        }
    }
}
