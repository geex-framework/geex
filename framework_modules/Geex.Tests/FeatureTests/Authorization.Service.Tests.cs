using Geex.Extensions.Authorization;
using Geex.Extensions.Authorization.Core.Casbin;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Geex.Tests.FeatureTests;

[Collection(nameof(TestsCollection))]
public class AuthorizationServiceTests : TestsBase
{
    public AuthorizationServiceTests(TestApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetUsersForRoleShouldReturnUsers()
    {
        using var scope = ScopedService.CreateScope();
        var enforcer = scope.ServiceProvider.GetRequiredService<IRbacEnforcer>();
        const string role = "test_role_users_for_role";
        const string user = "test_user_for_role";

        await enforcer.SetRoles(user, [role]);
        var users = enforcer.GetUsersForRole(role);
        users.ShouldContain(user);
    }
}
