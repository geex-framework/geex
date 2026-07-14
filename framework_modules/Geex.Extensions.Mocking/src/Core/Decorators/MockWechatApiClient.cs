using System.Text.Json.Nodes;
using Geex.Extensions.Authentication.Wechat.Core;
using Geex.Extensions.Mocking.Core.Entities;
using Geex.Storage;

namespace Geex.Extensions.Mocking.Core.Decorators;

public class MockWechatApiClient : IWechatApiClient
{
    private readonly IWechatApiClient _inner;
    private readonly IUnitOfWork _uow;

    public MockWechatApiClient(IWechatApiClient inner, IUnitOfWork uow)
    {
        _inner = inner;
        _uow = uow;
    }

    public async Task<WechatSessionResult> ExchangeWebCodeAsync(string appId, string appSecret, string code, CancellationToken cancellationToken = default)
    {
        var input = new JsonObject { ["appId"] = appId, ["code"] = code };
        var rule = _uow.FindMatchingRule(MockTargetEnum.WechatApiClient, nameof(ExchangeWebCodeAsync), input);
        if (rule is not null)
        {
            await rule.ApplyDelayAsync(cancellationToken);
            if (rule.Outcome == MockOutcomeEnum.Throw)
            {
                throw rule.CreateThrowException();
            }

            return rule.DeserializeResponse<WechatSessionResult>();
        }

        var authorization = _uow.Query<MockWechatAuthorization>().FirstOrDefault(x => x.Code == code);
        if (authorization is not null)
        {
            authorization.ConsumeCode(code);
            var profile = _uow.Query<MockWechatProfile>().FirstOrDefault(x => x.Id == authorization.ProfileId)
                          ?? throw new BusinessException(GeexExceptionType.NotFound, message: "Mock WeChat profile not found.");
            return new WechatSessionResult
            {
                OpenId = profile.OpenId,
                UnionId = profile.UnionId,
                AccessToken = $"mock-access-{authorization.Id}",
                ErrCode = "0",
            };
        }

        return await _inner.ExchangeWebCodeAsync(appId, appSecret, code, cancellationToken);
    }

    public async Task<WechatSessionResult> ExchangeMiniProgramCodeAsync(string appId, string appSecret, string code, CancellationToken cancellationToken = default)
    {
        var input = new JsonObject { ["appId"] = appId, ["code"] = code };
        var rule = _uow.FindMatchingRule(MockTargetEnum.WechatApiClient, nameof(ExchangeMiniProgramCodeAsync), input);
        if (rule is not null)
        {
            await rule.ApplyDelayAsync(cancellationToken);
            if (rule.Outcome == MockOutcomeEnum.Throw)
            {
                throw rule.CreateThrowException();
            }

            return rule.DeserializeResponse<WechatSessionResult>();
        }

        return await _inner.ExchangeMiniProgramCodeAsync(appId, appSecret, code, cancellationToken);
    }

    public async Task<Dictionary<string, string>?> GetWebUserInfoAsync(string accessToken, string openId, CancellationToken cancellationToken = default)
    {
        var input = new JsonObject { ["accessToken"] = accessToken, ["openId"] = openId };
        var rule = _uow.FindMatchingRule(MockTargetEnum.WechatApiClient, nameof(GetWebUserInfoAsync), input);
        if (rule is not null)
        {
            await rule.ApplyDelayAsync(cancellationToken);
            if (rule.Outcome == MockOutcomeEnum.Throw)
            {
                throw rule.CreateThrowException();
            }

            return rule.DeserializeResponse<Dictionary<string, string>>();
        }

        if (accessToken.StartsWith("mock-access-", StringComparison.Ordinal))
        {
            var profile = _uow.Query<MockWechatProfile>().FirstOrDefault(x => x.OpenId == openId && x.Enabled);
            return profile?.ToUserInfoDictionary();
        }

        return await _inner.GetWebUserInfoAsync(accessToken, openId, cancellationToken);
    }
}
