using Geex.Storage;
using Geex.Validation;

namespace Geex.Extensions.Mocking.Core.Entities;

public partial class MockWechatAuthorization : Entity<MockWechatAuthorization>
{
    public MockWechatAuthorization(string token, string redirectUri, string origin, string state, DateTimeOffset expiresAt)
    {
        Token = token;
        RedirectUri = redirectUri;
        Origin = origin;
        State = state;
        Status = MockWechatAuthorizationStatusEnum.Pending;
        ExpiresAt = expiresAt;
    }

    public string Token { get; private set; }
    public string RedirectUri { get; private set; }
    public string Origin { get; private set; }
    public string State { get; private set; }
    public MockWechatAuthorizationStatusEnum Status { get; private set; }
    public string? ProfileId { get; private set; }
    public string? Code { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? ConfirmedAt { get; private set; }
    public DateTimeOffset? ConsumedAt { get; private set; }

    public bool IsExpired => DateTimeOffset.Now >= ExpiresAt || Status == MockWechatAuthorizationStatusEnum.Expired;

    public void EnsureUsableForAuthorize()
    {
        if (IsExpired)
        {
            Status = MockWechatAuthorizationStatusEnum.Expired;
            throw new BusinessException(GeexExceptionType.ValidationFailed, message: "WeChat mock authorization expired.");
        }

        if (Status != MockWechatAuthorizationStatusEnum.Pending && Status != MockWechatAuthorizationStatusEnum.Confirmed)
        {
            throw new BusinessException(GeexExceptionType.ValidationFailed, message: $"WeChat mock authorization status [{Status}] is not usable.");
        }
    }

    public void RefreshExpiredStatus()
    {
        if (DateTimeOffset.Now >= ExpiresAt && Status == MockWechatAuthorizationStatusEnum.Pending)
        {
            Status = MockWechatAuthorizationStatusEnum.Expired;
        }
    }

    public void Confirm(string profileId)
    {
        EnsureUsableForAuthorize();
        if (Status == MockWechatAuthorizationStatusEnum.Confirmed && !string.IsNullOrEmpty(Code))
        {
            return;
        }

        ProfileId = profileId;
        Code = MockRule.CreateOpaqueToken(24);
        Status = MockWechatAuthorizationStatusEnum.Confirmed;
        ConfirmedAt = DateTimeOffset.Now;
    }

    public void ConsumeCode(string code)
    {
        if (IsExpired)
        {
            Status = MockWechatAuthorizationStatusEnum.Expired;
            throw new BusinessException(GeexExceptionType.ValidationFailed, message: "WeChat mock authorization expired.");
        }

        if (Status != MockWechatAuthorizationStatusEnum.Confirmed)
        {
            throw new BusinessException(GeexExceptionType.ValidationFailed, message: "WeChat mock authorization is not confirmed.");
        }

        if (!string.Equals(Code, code, StringComparison.Ordinal))
        {
            throw new BusinessException(GeexExceptionType.ValidationFailed, message: "WeChat mock code is invalid.");
        }

        Status = MockWechatAuthorizationStatusEnum.Consumed;
        ConsumedAt = DateTimeOffset.Now;
    }

    public override Task<ValidationResult> Validate(CancellationToken cancellation = default)
    {
        if (string.IsNullOrWhiteSpace(Token) || Token.Length < 22)
        {
            return Task.FromResult(new ValidationResult("Token must be an opaque value of at least 128 bits.", new[] { nameof(Token) }));
        }

        if (string.IsNullOrWhiteSpace(RedirectUri) || string.IsNullOrWhiteSpace(Origin))
        {
            return Task.FromResult(new ValidationResult("RedirectUri and Origin are required.", new[] { nameof(RedirectUri) }));
        }

        return Task.FromResult(ValidationResult.Success);
    }
}
