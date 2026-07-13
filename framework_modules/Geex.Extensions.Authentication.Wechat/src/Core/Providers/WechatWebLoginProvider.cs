using System.Security.Claims;
using Geex.Extensions.Authentication.Wechat.Core;

namespace Geex.Extensions.Authentication.Wechat
{
    public class WechatWebLoginProvider : ExternalLoginProviderBase
    {
        private readonly AuthenticationWechatModuleOptions _options;
        private readonly IWechatApiClient _wechatApiClient;

        public WechatWebLoginProvider(
            IExternalAccountLinker externalAccountLinker,
            AuthenticationWechatModuleOptions options,
            IWechatApiClient wechatApiClient) : base(externalAccountLinker)
        {
            _options = options;
            _wechatApiClient = wechatApiClient;
            _ = WechatLoginProviders.WechatWeb;
        }

        public override LoginProviderEnum Provider => WechatLoginProviders.WechatWeb;

        public override async Task<ExternalLoginIdentity> ResolveIdentity(string code)
        {
            var credentials = _options.Web
                ?? throw new BusinessException(GeexExceptionType.ValidationFailed, message: "未配置 AuthenticationModuleOptions:Wechat:Web.");
            if (string.IsNullOrWhiteSpace(credentials.AppId) || string.IsNullOrWhiteSpace(credentials.AppSecret))
            {
                throw new BusinessException(GeexExceptionType.ValidationFailed, message: "Wechat Web AppId/AppSecret 未配置.");
            }

            var session = await _wechatApiClient.ExchangeWebCodeAsync(credentials.AppId, credentials.AppSecret, code);
            if (session.IsError || string.IsNullOrWhiteSpace(session.OpenId))
            {
                throw new BusinessException(GeexExceptionType.ExternalError, message: $"微信网页登录换票失败: {session.ErrCode} {session.ErrMsg}");
            }

            var claims = new List<Claim> { new("openid", session.OpenId) };
            string? displayName = null;
            if (!string.IsNullOrWhiteSpace(session.AccessToken))
            {
                var userInfo = await _wechatApiClient.GetWebUserInfoAsync(session.AccessToken, session.OpenId);
                if (userInfo != null)
                {
                    foreach (var pair in userInfo)
                    {
                        claims.Add(new Claim(pair.Key, pair.Value));
                    }
                    userInfo.TryGetValue("nickname", out displayName);
                    if (string.IsNullOrWhiteSpace(session.UnionId) && userInfo.TryGetValue("unionid", out var unionId))
                    {
                        session.UnionId = unionId;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(session.UnionId))
            {
                claims.Add(new Claim("unionid", session.UnionId));
            }

            return new ExternalLoginIdentity
            {
                Provider = Provider,
                LoginProviderId = string.IsNullOrWhiteSpace(session.UnionId) ? session.OpenId : session.UnionId,
                DisplayName = displayName,
                Claims = claims,
            };
        }
    }
}
