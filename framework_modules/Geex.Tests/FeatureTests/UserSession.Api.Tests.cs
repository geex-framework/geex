using Geex.Abstractions;
using Geex.Extensions.Authentication;
using Geex.Extensions.Identity;
using Geex.Extensions.Identity.Requests;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using Shouldly;

namespace Geex.Tests.FeatureTests;

[Collection(nameof(TestsCollection))]
public class UserSessionApiTests : TestsBase
{
    public UserSessionApiTests(TestApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Authenticate_ShouldBeginSession()
    {
        var (_, username, passwordMd5) = await CreateTestUserAsync();

        var (responseData, _) = await AnonymousClient.PostGqlRequest(
            """
            mutation($userIdentifier: String!, $password: String!) {
                authenticate(request: { userIdentifier: $userIdentifier, password: $password }) {
                    token
                    userId
                    version
                    loginProvider
                }
            }
            """,
            new { userIdentifier = username, password = passwordMd5 });

        var session = responseData["data"]!["authenticate"]!;
        ((string)session["token"]!).ShouldNotBeNullOrEmpty();
        ((string)session["loginProvider"]!).ShouldBe(nameof(LoginProviderEnum.Local));
        session["version"]!.GetValue<long>().ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Authenticate_RepeatedLogin_ShouldIncrementVersion()
    {
        var (_, username, passwordMd5) = await CreateTestUserAsync();
        var mutation = """
            mutation($userIdentifier: String!, $password: String!) {
                authenticate(request: { userIdentifier: $userIdentifier, password: $password }) {
                    version
                }
            }
            """;
        var variables = new { userIdentifier = username, password = passwordMd5 };

        var (firstResp, _) = await AnonymousClient.PostGqlRequest(mutation, variables);
        var firstVersion = firstResp["data"]!["authenticate"]!["version"]!.GetValue<long>();

        var (secondResp, _) = await AnonymousClient.PostGqlRequest(mutation, variables);
        var secondVersion = secondResp["data"]!["authenticate"]!["version"]!.GetValue<long>();

        secondVersion.ShouldBeGreaterThan(firstVersion);
    }

    [Fact]
    public async Task CancelAuthentication_ShouldInvalidateSession()
    {
        var (userId, username, passwordMd5) = await CreateTestUserAsync();
        var authenticateMutation = """
            mutation($userIdentifier: String!, $password: String!) {
                authenticate(request: { userIdentifier: $userIdentifier, password: $password }) {
                    token
                    version
                }
            }
            """;

        var (loginResp, _) = await AnonymousClient.PostGqlRequest(
            authenticateMutation,
            new { userIdentifier = username, password = passwordMd5 });
        var token = (string)loginResp["data"]!["authenticate"]!["token"]!;
        var versionBeforeLogout = loginResp["data"]!["authenticate"]!["version"]!.GetValue<long>();

        var authedClient = AnonymousClient;
        authedClient.DefaultRequestHeaders.Add("Authorization", $"Local {token}");

        var (logoutResp, _) = await authedClient.PostGqlRequest(
            """
            mutation {
                cancelAuthentication
            }
            """);
        ((bool)logoutResp["data"]!["cancelAuthentication"]!).ShouldBeTrue();

        using (var scope = ScopedService.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var lastUpdatedOn = await uow.GetUserSession(userId).GetLastUpdatedOnAsync();
            lastUpdatedOn.ShouldBeGreaterThan(DateTimeOffset.MinValue);
        }

        var (reloginResp, _) = await AnonymousClient.PostGqlRequest(
            authenticateMutation,
            new { userIdentifier = username, password = passwordMd5 });
        var versionAfterRelogin = reloginResp["data"]!["authenticate"]!["version"]!.GetValue<long>();
        versionAfterRelogin.ShouldBeGreaterThan(versionBeforeLogout);
    }

    [Fact]
    public async Task GeneratePersonalAccessToken_WithExistingSession_ShouldWork()
    {
        var (_, username, passwordMd5) = await CreateTestUserAsync();
        var (loginResp, _) = await AnonymousClient.PostGqlRequest(
            """
            mutation($userIdentifier: String!, $password: String!) {
                authenticate(request: { userIdentifier: $userIdentifier, password: $password }) {
                    token
                }
            }
            """,
            new { userIdentifier = username, password = passwordMd5 });
        var loginToken = (string)loginResp["data"]!["authenticate"]!["token"]!;

        var authedClient = AnonymousClient;
        authedClient.DefaultRequestHeaders.Add("Authorization", $"Local {loginToken}");

        var patMutation = """
            mutation ($req: GeneratePersonalAccessTokenRequest!) {
                generatePersonalAccessToken(request: $req) {
                    token
                    userId
                }
            }
            """;

        var (firstPatResp, _) = await authedClient.PostGqlRequest(patMutation, new { req = new { expireInSeconds = 600 } });
        var firstPat = (string)firstPatResp["data"]!["generatePersonalAccessToken"]!["token"]!;
        firstPat.ShouldNotBeNullOrEmpty();

        var patClient = AnonymousClient;
        patClient.DefaultRequestHeaders.Add("Authorization", $"Local {firstPat}");
        var (secondPatResp, _) = await patClient.PostGqlRequest(patMutation, new { req = new { expireInSeconds = 30 } });
        ((string)secondPatResp["data"]!["generatePersonalAccessToken"]!["token"]!).ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task RoleChange_ShouldInvalidateSession()
    {
        var (userId, username, passwordMd5) = await CreateTestUserAsync();
        var roleId = await CreateTestRoleAsync();
        var authenticateMutation = """
            mutation($userIdentifier: String!, $password: String!) {
                authenticate(request: { userIdentifier: $userIdentifier, password: $password }) {
                    version
                }
            }
            """;
        var loginVariables = new { userIdentifier = username, password = passwordMd5 };

        var (loginResp, _) = await AnonymousClient.PostGqlRequest(authenticateMutation, loginVariables);
        var versionBeforeRoleChange = loginResp["data"]!["authenticate"]!["version"]!.GetValue<long>();

        await SuperAdminClient.PostGqlRequest(
            """
            mutation($id: String!, $roleIds: [String!]) {
                editUser(request: { id: $id, roleIds: $roleIds, nickname: "session-role-test" }) {
                    id
                }
            }
            """,
            new { id = userId, roleIds = new[] { roleId } });

        var (reloginResp, _) = await AnonymousClient.PostGqlRequest(authenticateMutation, loginVariables);
        var versionAfterRoleChange = reloginResp["data"]!["authenticate"]!["version"]!.GetValue<long>();
        versionAfterRoleChange.ShouldBeGreaterThan(versionBeforeRoleChange);
    }

    private async Task<(string userId, string username, string passwordMd5)> CreateTestUserAsync()
    {
        var username = $"session_{ObjectId.GenerateNewId()}";
        var passwordMd5 = "Password123!".ToMd5();
        string userId;

        using (var scope = ScopedService.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var user = await uow.Request(new CreateUserRequest
            {
                Username = username,
                Email = $"{username}@test.com",
                Password = passwordMd5,
                Nickname = "Session Test User",
                IsEnable = true,
                RoleIds = [],
                OrgCodes = []
            });
            await uow.SaveChanges();
            userId = user.Id;
        }

        return (userId, username, passwordMd5);
    }

    private async Task<string> CreateTestRoleAsync()
    {
        var roleCode = $"sessionrole_{ObjectId.GenerateNewId()}";
        using var scope = ScopedService.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var role = await uow.Request(new CreateRoleRequest
        {
            RoleCode = roleCode,
            RoleName = $"Session Role {ObjectId.GenerateNewId()}",
            IsStatic = false,
            IsDefault = false
        });
        await uow.SaveChanges();
        return role.Id;
    }
}
