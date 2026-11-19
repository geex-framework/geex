using System.Security.Claims;
using System.Security.Principal;
using Geex.Extensions.Authentication;
using Geex.Extensions.Authentication.Core.Entities;
using Geex.Extensions.Identity;
using Geex.Extensions.MultiTenant.Api;
using Xunit;

namespace Geex.Tests.UnitTests;

public sealed class ClaimIdentityExtensionsTests
{
    [Fact]
    public void Subject_wire_key_resolves_identity_and_principal_user_id()
    {
        var identity = new ClaimsIdentity([
            new Claim("Sub", "alias-user"),
            new Claim("client_id", "client-17"),
            new Claim("sub", "user-17"),
        ], "Test");

        Assert.Equal("user-17", identity.FindUserId());
        Assert.Equal("user-17", new ClaimsPrincipal(identity).FindUserId());
    }

    [Fact]
    public void Provider_wire_key_selects_the_nonlocal_login_provider()
    {
        var identity = new ClaimsIdentity([
            new Claim("Provider", "Local"),
            new Claim("login_provider", "PersonalAccessToken"),
        ], "Test");

        Assert.Equal(LoginProviderEnum.PersonalAccessToken, identity.GetLoginProvider());
    }

    [Fact]
    public void Organization_wire_keys_resolve_principal_and_current_user_membership()
    {
        var identity = new ClaimsIdentity([
            new Claim("role", "role-only"),
            new Claim("Org", "alias-org"),
            new Claim("org", "ORG-A"),
            new Claim("org", "ORG-B"),
        ], "Test");
        ICurrentUser currentUser = new ClaimsCurrentUser(identity);

        Assert.Equal(new[] { "ORG-A", "ORG-B" }, new ClaimsPrincipal(identity).FindOrgCodes());
        Assert.Equal(new[] { "ORG-A", "ORG-B" }, currentUser.GetOrgCodes());
    }

    [Fact]
    public void Tenant_wire_key_resolves_identity_and_principal_tenant_code()
    {
        var identity = new ClaimsIdentity([
            new Claim("Tenant", "alias-tenant"),
            new Claim("__tenant", "tenant-a"),
        ], "Test");

        Assert.Equal("tenant-a", identity.FindTenantCode());
        Assert.Equal("tenant-a", new ClaimsPrincipal(identity).FindTenantCode());
    }

    [Fact]
    public void Subject_and_tenant_do_not_fall_back_to_unrelated_claims_or_identity_names()
    {
        var identity = new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, "unrelated-user"),
            new Claim("Sub", "alias-user"),
            new Claim("Tenant", "alias-tenant"),
            new Claim("client_id", "client-17"),
        ], "Test");
        var principal = new ClaimsPrincipal(identity);
        IIdentity namedIdentity = new GenericIdentity("identity-name", "Test");

        Assert.Null(identity.FindUserId());
        Assert.Null(principal.FindUserId());
        Assert.Null(identity.FindTenantCode());
        Assert.Null(principal.FindTenantCode());
        Assert.Null(namedIdentity.FindUserId());
        Assert.Null(namedIdentity.FindTenantCode());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Missing_provider_preserves_the_local_login_default(bool hasIdentity)
    {
        ClaimsIdentity? identity = hasIdentity
            ? new ClaimsIdentity([new Claim("Provider", "PersonalAccessToken")], "Test")
            : null;

        Assert.Equal(LoginProviderEnum.Local, identity.GetLoginProvider());
    }

    [Fact]
    public void Organization_readers_return_no_membership_without_the_org_wire_key()
    {
        var identity = new ClaimsIdentity([
            new Claim("role", "ORG-A"),
            new Claim("Org", "alias-org"),
        ], "Test");
        ICurrentUser currentUser = new ClaimsCurrentUser(identity);

        Assert.Empty(new ClaimsPrincipal(identity).FindOrgCodes()!);
        Assert.Empty(currentUser.GetOrgCodes()!);
    }

    private sealed class ClaimsCurrentUser(ClaimsIdentity identity) : ICurrentUser
    {
        public string? UserId => null;
        public IAuthUser? User => null;
        public ClaimsIdentity ClaimsIdentity => identity;
        public UserSession? Session => null;
        public IDisposable Change(string? userId) => throw new NotSupportedException();
    }
}
