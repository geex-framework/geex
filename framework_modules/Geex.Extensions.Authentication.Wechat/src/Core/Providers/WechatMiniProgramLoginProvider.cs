using System.Security.Claims;
using Geex.Extensions.Authentication.Wechat.Core;

namespace Geex.Extensions.Authentication.Wechat
{
    public class WechatMiniProgramLoginProvider : ExternalLoginProviderBase
    {
        private readonly AuthenticationWechatModuleOptions _options;
        private readonly IWechatApiClient _wechatApiClient;

        public WechatMiniProgramLoginProvider(
            IExternalAccountLinker externalAccountLinker,
            AuthenticationWechatModuleOptions options,
            IWechatApiClient wechatApiClient) : base(externalAccountLinker)
        {
            _options = options;
            _wechatApiClient = wechatApiClient;
            _ = WechatLoginProviders.WechatMiniProgram;
        }

        public override LoginProviderEnum Provider => WechatLoginProviders.WechatMiniProgram;

        public override async Task<ExternalLoginIdentity> ResolveIdentity(string code)
        {
            var credentials = _options.MiniProgram
                ?? throw new BusinessException(GeexExceptionType.ValidationFailed, message: "未配置 AuthenticationModuleOptions:Wechat:MiniProgram.");
            if (string.IsNullOrWhiteSpace(credentials.AppId) || string.IsNullOrWhiteSpace(credentials.AppSecret))
            {
                throw new BusinessException(GeexExceptionType.ValidationFailed, message: "Wechat MiniProgram AppId/AppSecret 未配置.");
            }

            var session = await _wechatApiClient.ExchangeMiniProgramCodeAsync(credentials.AppId, credentials.AppSecret, code);
            if (session.IsError || string.IsNullOrWhiteSpace(session.OpenId))
            {
                throw new BusinessException(GeexExceptionType.ExternalError, message: $"微信小程序登录换票失败: {session.ErrCode} {session.ErrMsg}");
            }

            var claims = new List<Claim> { new("openid", session.OpenId) };
            if (!string.IsNullOrWhiteSpace(session.UnionId))
            {
                claims.Add(new Claim("unionid", session.UnionId));
            }

            return new ExternalLoginIdentity
            {
                Provider = Provider,
                LoginProviderId = string.IsNullOrWhiteSpace(session.UnionId) ? session.OpenId : session.UnionId,
                DisplayName = null,
                Claims = claims,
            };
        }
    }
}
