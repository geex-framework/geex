using Geex.Abstractions;
using Geex.Extensions.Authentication;
using Geex.Extensions.Authentication.Core.Entities;
using Geex.Extensions.Identity;
using Geex.Extensions.Identity.Requests;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using Shouldly;
using StackExchange.Redis.Extensions.Core;
using StackExchange.Redis.Extensions.Core.Abstractions;

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
                    lastUpdatedOn
                    loginProvider
                }
            }
            """,
            new { userIdentifier = username, password = passwordMd5 });

        var session = responseData["data"]!["authenticate"]!;
        ((string)session["token"]!).ShouldNotBeNullOrEmpty();
        ((string)session["loginProvider"]!).ShouldBe(nameof(LoginProviderEnum.Local));
        session["lastUpdatedOn"]!.GetValue<DateTimeOffset>().ShouldBeGreaterThan(DateTimeOffset.MinValue);
    }

    [Fact]
    public async Task Authenticate_RepeatedLogin_ShouldUpdateLastUpdatedOn()
    {
        var (userId, username, passwordMd5) = await CreateTestUserAsync();
        var mutation = """
            mutation($userIdentifier: String!, $password: String!) {
                authenticate(request: { userIdentifier: $userIdentifier, password: $password }) {
                    token
                    lastUpdatedOn
                }
            }
            """;
        var variables = new { userIdentifier = username, password = passwordMd5 };

        var (firstResp, _) = await AnonymousClient.PostGqlRequest(mutation, variables);
        var firstToken = (string)firstResp["data"]!["authenticate"]!["token"]!;
        var firstLastUpdatedOn = firstResp["data"]!["authenticate"]!["lastUpdatedOn"]!.GetValue<DateTimeOffset>();

        await Task.Delay(10);

        var (secondResp, _) = await AnonymousClient.PostGqlRequest(mutation, variables);
        var secondToken = (string)secondResp["data"]!["authenticate"]!["token"]!;
        var secondLastUpdatedOn = secondResp["data"]!["authenticate"]!["lastUpdatedOn"]!.GetValue<DateTimeOffset>();

        secondLastUpdatedOn.ShouldBeGreaterThan(firstLastUpdatedOn);
        secondToken.ShouldNotBe(firstToken);

        using var scope = ScopedService.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var dbSession = uow.Query<UserSession>()
            .FirstOrDefault(x => x.UserId == userId && x.LoginProvider == LoginProviderEnum.Local);
        dbSession.ShouldNotBeNull();
        dbSession.Token.ShouldBe(secondToken);
        dbSession.LastUpdatedOn.ShouldBe(secondLastUpdatedOn);
    }

    [Fact]
    public async Task CancelAuthentication_ShouldInvalidateSessionCache()
    {
        var (userId, username, passwordMd5) = await CreateTestUserAsync();
        var authenticateMutation = """
            mutation($userIdentifier: String!, $password: String!) {
                authenticate(request: { userIdentifier: $userIdentifier, password: $password }) {
                    token
                    lastUpdatedOn
                }
            }
            """;

        var (loginResp, _) = await AnonymousClient.PostGqlRequest(
            authenticateMutation,
            new { userIdentifier = username, password = passwordMd5 });
        var token = (string)loginResp["data"]!["authenticate"]!["token"]!;
        var lastUpdatedOnBeforeLogout = loginResp["data"]!["authenticate"]!["lastUpdatedOn"]!.GetValue<DateTimeOffset>();

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
            var redis = scope.ServiceProvider.GetRequiredService<IRedisDatabase>();
            var cacheKey = UserSession.GetCacheKey(userId, LoginProviderEnum.Local);
            var cached = await redis.GetNamedAsync<UserSession>(cacheKey);
            cached.ShouldBeNull();

            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var dbSession = uow.Query<UserSession>()
                .FirstOrDefault(x => x.UserId == userId && x.LoginProvider == LoginProviderEnum.Local);
            dbSession.ShouldNotBeNull();
            dbSession.LastUpdatedOn.ShouldBe(lastUpdatedOnBeforeLogout);
        }

        var (reloginResp, _) = await AnonymousClient.PostGqlRequest(
            authenticateMutation,
            new { userIdentifier = username, password = passwordMd5 });
        var lastUpdatedOnAfterRelogin = reloginResp["data"]!["authenticate"]!["lastUpdatedOn"]!.GetValue<DateTimeOffset>();
        lastUpdatedOnAfterRelogin.ShouldBeGreaterThan(lastUpdatedOnBeforeLogout);
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
    public async Task RoleChange_ShouldInvalidateSessionCache()
    {
        var (userId, username, passwordMd5) = await CreateTestUserAsync();
        var roleId = await CreateTestRoleAsync();
        var authenticateMutation = """
            mutation($userIdentifier: String!, $password: String!) {
                authenticate(request: { userIdentifier: $userIdentifier, password: $password }) {
                    token
                    lastUpdatedOn
                }
            }
            """;
        var loginVariables = new { userIdentifier = username, password = passwordMd5 };

        var (loginResp, _) = await AnonymousClient.PostGqlRequest(authenticateMutation, loginVariables);
        var token = (string)loginResp["data"]!["authenticate"]!["token"]!;
        var lastUpdatedOnBeforeRoleChange = loginResp["data"]!["authenticate"]!["lastUpdatedOn"]!.GetValue<DateTimeOffset>();

        await SuperAdminClient.PostGqlRequest(
            """
            mutation($id: String!, $roleIds: [String!]) {
                editUser(request: { id: $id, roleIds: $roleIds, nickname: "session-role-test" }) {
                    id
                }
            }
            """,
            new { id = userId, roleIds = new[] { roleId } });

        using (var scope = ScopedService.CreateScope())
        {
            var redis = scope.ServiceProvider.GetRequiredService<IRedisDatabase>();
            var cacheKey = UserSession.GetCacheKey(userId, LoginProviderEnum.Local);
            var cached = await redis.GetNamedAsync<UserSession>(cacheKey);
            cached.ShouldBeNull();
        }

        var (reloginResp, _) = await AnonymousClient.PostGqlRequest(authenticateMutation, loginVariables);
        var lastUpdatedOnAfterRoleChange = reloginResp["data"]!["authenticate"]!["lastUpdatedOn"]!.GetValue<DateTimeOffset>();
        lastUpdatedOnAfterRoleChange.ShouldBeGreaterThan(lastUpdatedOnBeforeRoleChange);
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
