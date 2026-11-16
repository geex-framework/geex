using System.Security.Claims;
using Geex.Extensions.Authentication;
using Geex.Extensions.Authentication.Core.Entities;
using Geex.Extensions.Identity;
using Geex.Extensions.MultiTenant.Api;
using Xunit;

namespace Geex.Tests.UnitTests;

public class ClaimsLookupTests
{
    [Fact]
    public void MissingOrEmptyProviderDefaultsToLocal()
    {
        ClaimsIdentity? missing = null;
        Assert.Same(LoginProviderEnum.Local, missing.GetLoginProvider());
        Assert.Same(LoginProviderEnum.Local, new ClaimsIdentity().GetLoginProvider());
        Assert.Same(LoginProviderEnum.Local,
            new ClaimsIdentity([new Claim(GeexClaimType._Provider, "")]).GetLoginProvider());
    }

    [Theory]
    [InlineData(nameof(LoginProviderEnum.Local))]
    [InlineData(nameof(LoginProviderEnum.PersonalAccessToken))]
    public void KnownProviderIsPreserved(string providerValue)
    {
        var expected = LoginProviderEnum.FromValue(providerValue);
        var identity = new ClaimsIdentity([new Claim(GeexClaimType._Provider, providerValue)]);

        Assert.Same(expected, identity.GetLoginProvider());
    }

    [Fact]
    public void ProviderLookupDoesNotRegisterUnrelatedClaimTypes()
    {
        var unknownType = "oidc_claim_" + Guid.NewGuid().ToString("N");
        var expected = LoginProviderEnum.PersonalAccessToken;
        var identity = new ClaimsIdentity([
            new Claim(unknownType, "openid"),
            new Claim("oi_scp", "profile"),
            new Claim(GeexClaimType._Provider, expected.Value),
        ]);
        Assert.Null(GeexClaimType.FromExistedValue(unknownType, noException: true));

        Assert.Same(expected, identity.GetLoginProvider());

        Assert.Null(GeexClaimType.FromExistedValue(unknownType, noException: true));
    }

    [Fact]
    public async Task ConcurrentProviderLookupsDoNotRegisterUnrelatedClaimTypes()
    {
        var unknownType = "oidc_concurrent_" + Guid.NewGuid().ToString("N");
        var expected = LoginProviderEnum.PersonalAccessToken;
        var start = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var lookups = Enumerable.Range(0, 3).Select(_ => Task.Run(async () =>
        {
            await start.Task;
            var identity = new ClaimsIdentity([
                new Claim(unknownType, "openid"),
                new Claim(GeexClaimType._Provider, expected.Value),
            ]);
            return identity.GetLoginProvider();
        })).ToArray();

        start.SetResult(true);
        var providers = await Task.WhenAll(lookups);

        Assert.All(providers, provider => Assert.Same(expected, provider));
        Assert.Null(GeexClaimType.FromExistedValue(unknownType, noException: true));
    }

    [Theory]
    [InlineData("user")]
    [InlineData("tenant")]
    [InlineData("principal-orgs")]
    [InlineData("current-user-orgs")]
    public void IdentityLookupsDoNotRegisterUnrelatedClaimTypes(string lookup)
    {
        var unknownType = "external_claim_" + Guid.NewGuid().ToString("N");
        var identity = new ClaimsIdentity([
            new Claim(unknownType, "external"),
            new Claim(GeexClaimType._Sub, "user-1"),
            new Claim(GeexClaimType._Tenant, "tenant-1"),
            new Claim(GeexClaimType._Org, "org-1"),
            new Claim(GeexClaimType._Org, "org-2"),
        ]);
        var principal = new ClaimsPrincipal(identity);
        Assert.Null(GeexClaimType.FromExistedValue(unknownType, noException: true));

        switch (lookup)
        {
            case "user":
                Assert.Equal("user-1", principal.FindUserId());
                break;
            case "tenant":
                Assert.Equal("tenant-1", principal.FindTenantCode());
                break;
            case "principal-orgs":
                Assert.Equal(new[] { "org-1", "org-2" }, principal.FindOrgCodes());
                break;
            case "current-user-orgs":
                Assert.Equal(new[] { "org-1", "org-2" }, new ClaimsCurrentUser(identity).GetOrgCodes());
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(lookup));
        }

        Assert.Null(GeexClaimType.FromExistedValue(unknownType, noException: true));
    }

    private sealed class ClaimsCurrentUser(ClaimsIdentity identity) : ICurrentUser
    {
        public string? UserId => identity.FindUserId();
        public IAuthUser? User => null;
        public ClaimsIdentity ClaimsIdentity => identity;
        public UserSession? Session => null;
        public IDisposable Change(string? userId) => throw new NotSupportedException();
    }
}
