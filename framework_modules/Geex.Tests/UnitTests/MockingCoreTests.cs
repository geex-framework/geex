using System.Text.Json.Nodes;
using Geex.Extensions.Authentication;
using Geex.Extensions.Mocking;
using Geex.Extensions.Mocking.Core.Entities;
using Shouldly;
using Xunit;

namespace Geex.Tests.UnitTests;

public class MockRuleMatchingTests
{
    [Fact]
    public void Matches_EmptyMatch_MatchesAnyInput()
    {
        var rule = new MockRule("r1", MockTargetEnum.SmsSender, "SendAsync", MockOutcomeEnum.Return);
        rule.Matches(MockTargetEnum.SmsSender, "SendAsync", new JsonObject { ["phoneNumber"] = "13800000000" }).ShouldBeTrue();
    }

    [Fact]
    public void Matches_JsonSubset_RequiresExpectedFields()
    {
        var rule = new MockRule(
            "r1",
            MockTargetEnum.WechatApiClient,
            "ExchangeWebCodeAsync",
            MockOutcomeEnum.Return,
            match: new JsonObject { ["appId"] = "wx_demo" });

        rule.Matches(MockTargetEnum.WechatApiClient, "ExchangeWebCodeAsync", new JsonObject
        {
            ["appId"] = "wx_demo",
            ["code"] = "abc",
        }).ShouldBeTrue();

        rule.Matches(MockTargetEnum.WechatApiClient, "ExchangeWebCodeAsync", new JsonObject
        {
            ["appId"] = "other",
            ["code"] = "abc",
        }).ShouldBeFalse();
    }

    [Fact]
    public void SelectBest_UsesHigherPriority()
    {
        var low = new MockRule("low", MockTargetEnum.SmsSender, "SendAsync", MockOutcomeEnum.Return, priority: 1);
        var high = new MockRule("high", MockTargetEnum.SmsSender, "SendAsync", MockOutcomeEnum.Throw, priority: 10);

        var selected = MockRule.SelectBest(new[] { low, high }, MockTargetEnum.SmsSender, "SendAsync", null);
        selected.ShouldBe(high);
    }

    [Fact]
    public void Matches_WrongOperation_ReturnsFalse()
    {
        var rule = new MockRule("r1", MockTargetEnum.SmsSender, "SendAsync", MockOutcomeEnum.Return);
        rule.Matches(MockTargetEnum.SmsSender, "Other", null).ShouldBeFalse();
    }
}

public class MockInteractionTokenTests
{
    [Fact]
    public void CreateOpaqueToken_HasAtLeast128Bits()
    {
        var token = MockRule.CreateOpaqueToken();
        token.Length.ShouldBeGreaterThanOrEqualTo(22);
    }

    [Fact]
    public void WechatAuthorization_ConfirmThenConsume_IsOneTime()
    {
        var auth = new MockWechatAuthorization(
            MockRule.CreateOpaqueToken(),
            "https://admin.example.test/auth/login",
            "https://admin.example.test",
            "WechatWeb",
            DateTimeOffset.Now.AddMinutes(5));

        auth.Confirm("profile1");
        auth.Status.ShouldBe(MockWechatAuthorizationStatusEnum.Confirmed);
        auth.Code.ShouldNotBeNullOrWhiteSpace();

        var code = auth.Code!;
        auth.ConsumeCode(code);
        auth.Status.ShouldBe(MockWechatAuthorizationStatusEnum.Consumed);

        Should.Throw<BusinessException>(() => auth.ConsumeCode(code));
    }

    [Fact]
    public void WechatAuthorization_ExpiredToken_CannotConfirm()
    {
        var auth = new MockWechatAuthorization(
            MockRule.CreateOpaqueToken(),
            "https://admin.example.test/auth/login",
            "https://admin.example.test",
            "WechatWeb",
            DateTimeOffset.Now.AddMinutes(-1));

        Should.Throw<BusinessException>(() => auth.Confirm("profile1"));
        auth.Status.ShouldBe(MockWechatAuthorizationStatusEnum.Expired);
    }

    [Fact]
    public void PaymentTransaction_TokenPurposeIsolation_SucceedsOnce()
    {
        var tx = new MockPaymentTransaction(
            MockRule.CreateOpaqueToken(),
            "PAY001",
            "https://admin.example.test",
            1.23m,
            "CNY",
            "demo",
            Geex.Extensions.Payments.PaymentProviderEnum.Virtual,
            Geex.Extensions.Payments.PaymentChannelEnum.Precreate,
            DateTimeOffset.Now.AddMinutes(10));

        tx.ConfirmSucceeded();
        tx.Status.ShouldBe(MockPaymentTransactionStatusEnum.Succeeded);
        Should.Throw<BusinessException>(() => tx.ConfirmFailed());
    }
}

public class MockWechatProfileUserInfoTests
{
    [Fact]
    public void ToUserInfoDictionary_IncludesOpenIdAndNickname()
    {
        var profile = new MockWechatProfile("wx_open_1", "nick", "union_1", "https://avatar");
        var dict = profile.ToUserInfoDictionary();
        dict["openid"].ShouldBe("wx_open_1");
        dict["nickname"].ShouldBe("nick");
        dict["unionid"].ShouldBe("union_1");
        dict["avatar"].ShouldBe("https://avatar");
    }

    [Fact]
    public void ToUserInfoDictionary_DoesNotDuplicateOpenIdWhenClaimsContainOpenId()
    {
        var profile = new MockWechatProfile(
            "wx_open_1",
            "nick",
            claims: new JsonObject { ["openid"] = "wx_open_1", ["city"] = "SZ" });
        var dict = profile.ToUserInfoDictionary();
        dict["openid"].ShouldBe("wx_open_1");
        dict["city"].ShouldBe("SZ");
        dict.Count(x => x.Key.Equals("openid", StringComparison.OrdinalIgnoreCase)).ShouldBe(1);
    }
}

public class MockingSuperAdminGuardTests
{
    [Fact]
    public void EnsureSuperAdmin_RejectsNonSuperAdmin()
    {
        var user = new FakeCurrentUser("normal-user");
        Should.Throw<BusinessException>(() => user.EnsureSuperAdmin());
    }

    [Fact]
    public void EnsureSuperAdmin_AllowsSuperAdmin()
    {
        var user = new FakeCurrentUser(GeexConstants.SuperAdminId);
        Should.NotThrow(() => user.EnsureSuperAdmin());
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public FakeCurrentUser(string? userId) => UserId = userId;
        public string? UserId { get; }
        public IAuthUser? User => null;
        public System.Security.Claims.ClaimsIdentity ClaimsIdentity => new();
        public Geex.Extensions.Authentication.Core.Entities.UserSession? Session => null;
        public IDisposable Change(string? userId) => new Noop();
        private sealed class Noop : IDisposable { public void Dispose() { } }
    }
}
