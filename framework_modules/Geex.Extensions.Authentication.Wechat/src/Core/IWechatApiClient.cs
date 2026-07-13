namespace Geex.Extensions.Authentication.Wechat.Core
{
    public class WechatSessionResult
    {
        public string? OpenId { get; set; }
        public string? UnionId { get; set; }
        public string? AccessToken { get; set; }
        public string? ErrCode { get; set; }
        public string? ErrMsg { get; set; }

        public bool IsError => !string.IsNullOrEmpty(ErrCode) && ErrCode != "0";
    }

    public interface IWechatApiClient
    {
        Task<WechatSessionResult> ExchangeWebCodeAsync(string appId, string appSecret, string code, CancellationToken cancellationToken = default);
        Task<WechatSessionResult> ExchangeMiniProgramCodeAsync(string appId, string appSecret, string code, CancellationToken cancellationToken = default);
        Task<Dictionary<string, string>?> GetWebUserInfoAsync(string accessToken, string openId, CancellationToken cancellationToken = default);
    }
}
