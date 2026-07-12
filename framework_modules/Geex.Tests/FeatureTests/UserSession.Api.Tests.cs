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
    public async Task Authenticate_RepeatedLogin_ShouldRejectOldToken()
    {
        var (_, username, passwordMd5) = await CreateTestUserAsync();
        var mutation = """
            mutation($userIdentifier: String!, $password: String!) {
                authenticate(request: { userIdentifier: $userIdentifier, password: $password }) {
                    token
                }
            }
            """;
        var variables = new { userIdentifier = username, password = passwordMd5 };

        var (firstResp, _) = await AnonymousClient.PostGqlRequest(mutation, variables);
        var firstToken = (string)firstResp["data"]!["authenticate"]!["token"]!;

        var (secondResp, _) = await AnonymousClient.PostGqlRequest(mutation, variables);
        var secondToken = (string)secondResp["data"]!["authenticate"]!["token"]!;
        secondToken.ShouldNotBe(firstToken);

        var oldTokenClient = AnonymousClient;
        oldTokenClient.DefaultRequestHeaders.Add("Authorization", $"Local {firstToken}");
        var (_, oldTokenResponse) = await oldTokenClient.PostGqlRequest(
            """
            mutation {
                generatePersonalAccessToken(request: { expireInSeconds: 60 }) {
                    token
                }
            }
            """,
            ignoreError: true);
        oldTokenResponse.ShouldContain("errors");

        var newTokenClient = AnonymousClient;
        newTokenClient.DefaultRequestHeaders.Add("Authorization", $"Local {secondToken}");
        var (newTokenResp, newTokenResponse) = await newTokenClient.PostGqlRequest(
            """
            mutation {
                generatePersonalAccessToken(request: { expireInSeconds: 60 }) {
                    token
                }
            }
            """);
        newTokenResponse.ShouldNotContain("errors");
        ((string)newTokenResp["data"]!["generatePersonalAccessToken"]!["token"]!).ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task CancelAuthentication_ShouldDeleteSessionAndRejectOldToken()
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
            dbSession.ShouldBeNull();
        }

        var (_, afterLogoutResponse) = await authedClient.PostGqlRequest(
            """
            mutation {
                generatePersonalAccessToken(request: { expireInSeconds: 60 }) {
                    token
                }
            }
            """,
            ignoreError: true);
        afterLogoutResponse.ShouldContain("errors");

        var (reloginResp, _) = await AnonymousClient.PostGqlRequest(
            authenticateMutation,
            new { userIdentifier = username, password = passwordMd5 });
        var newToken = (string)reloginResp["data"]!["authenticate"]!["token"]!;
        newToken.ShouldNotBeNullOrEmpty();
        newToken.ShouldNotBe(token);

        var reloginClient = AnonymousClient;
        reloginClient.DefaultRequestHeaders.Add("Authorization", $"Local {newToken}");
        var (patResp, patResponse) = await reloginClient.PostGqlRequest(
            """
            mutation {
                generatePersonalAccessToken(request: { expireInSeconds: 60 }) {
                    token
                }
            }
            """);
        patResponse.ShouldNotContain("errors");
        ((string)patResp["data"]!["generatePersonalAccessToken"]!["token"]!).ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task GeneratePersonalAccessToken_ShouldKeepLoginTokenValid()
    {
        var (_, username, passwordMd5) = await CreateTestUserAsync();
        var (loginResp, _) = await AnonymousClient.PostGqlRequest(
            """
            mutation($userIdentifier: String!, $password: String!) {
                authenticate(request: { userIdentifier: $userIdentifier, password: $password }) {
                    token
                    loginProvider
                }
            }
            """,
            new { userIdentifier = username, password = passwordMd5 });
        var loginToken = (string)loginResp["data"]!["authenticate"]!["token"]!;
        ((string)loginResp["data"]!["authenticate"]!["loginProvider"]!).ShouldBe(nameof(LoginProviderEnum.Local));

        var loginClient = AnonymousClient;
        loginClient.DefaultRequestHeaders.Add("Authorization", $"Local {loginToken}");

        var patMutation = """
            mutation ($req: GeneratePersonalAccessTokenRequest!) {
                generatePersonalAccessToken(request: $req) {
                    token
                    userId
                    loginProvider
                }
            }
            """;

        var (firstPatResp, _) = await loginClient.PostGqlRequest(patMutation, new { req = new { expireInSeconds = 600 } });
        var firstPat = (string)firstPatResp["data"]!["generatePersonalAccessToken"]!["token"]!;
        firstPat.ShouldNotBeNullOrEmpty();
        firstPat.ShouldNotBe(loginToken);
        ((string)firstPatResp["data"]!["generatePersonalAccessToken"]!["loginProvider"]!)
            .ShouldBe(nameof(LoginProviderEnum.PersonalAccessToken));

        var (stillValidLoginResp, stillValidLoginResponse) = await loginClient.PostGqlRequest(patMutation, new { req = new { expireInSeconds = 120 } });
        stillValidLoginResponse.ShouldNotContain("errors");
        var secondPat = (string)stillValidLoginResp["data"]!["generatePersonalAccessToken"]!["token"]!;
        secondPat.ShouldNotBe(firstPat);

        var expiredPatClient = AnonymousClient;
        expiredPatClient.DefaultRequestHeaders.Add("Authorization", $"Local {firstPat}");
        var (_, expiredPatResponse) = await expiredPatClient.PostGqlRequest(
            """
            mutation {
                generatePersonalAccessToken(request: { expireInSeconds: 60 }) {
                    token
                }
            }
            """,
            ignoreError: true);
        expiredPatResponse.ShouldContain("errors");

        var patClient = AnonymousClient;
        patClient.DefaultRequestHeaders.Add("Authorization", $"Local {secondPat}");
        var (cancelPatResp, cancelPatResponse) = await patClient.PostGqlRequest(
            """
            mutation {
                cancelAuthentication
            }
            """);
        cancelPatResponse.ShouldNotContain("errors");
        ((bool)cancelPatResp["data"]!["cancelAuthentication"]!).ShouldBeTrue();

        var (loginAfterPatCancelResp, loginAfterPatCancelResponse) = await loginClient.PostGqlRequest(
            """
            mutation {
                generatePersonalAccessToken(request: { expireInSeconds: 60 }) {
                    token
                    loginProvider
                }
            }
            """);
        loginAfterPatCancelResponse.ShouldNotContain("errors");
        ((string)loginAfterPatCancelResp["data"]!["generatePersonalAccessToken"]!["loginProvider"]!)
            .ShouldBe(nameof(LoginProviderEnum.PersonalAccessToken));
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

        var authedClient = AnonymousClient;
        authedClient.DefaultRequestHeaders.Add("Authorization", $"Local {token}");
        var (patResp, patResponse) = await authedClient.PostGqlRequest(
            """
            mutation {
                generatePersonalAccessToken(request: { expireInSeconds: 60 }) {
                    token
                }
            }
            """);
        patResponse.ShouldNotContain("errors");
        ((string)patResp["data"]!["generatePersonalAccessToken"]!["token"]!).ShouldNotBeNullOrEmpty();

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
