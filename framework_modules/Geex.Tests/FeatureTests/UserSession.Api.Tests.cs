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
using System.Text.Json.Nodes;

namespace Geex.Tests.FeatureTests;

[Collection(nameof(TestsCollection))]
public class UserSessionApiTests : TestsBase
{
    public UserSessionApiTests(TestApplicationFactory factory) : base(factory)
    {
    }

    public new HttpClient AnonymousClient => Record(base.AnonymousClient);
    public new HttpClient SuperAdminClient => Record(base.SuperAdminClient);

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

        using var authedClient = AnonymousClient;
        authedClient.DefaultRequestHeaders.Add("Authorization", $"Local {token}");
        var (warmupResp, warmupResponse) = await authedClient.PostGqlRequest(
            """
            mutation {
                generatePersonalAccessToken(request: { expireInSeconds: 60 }) {
                    token
                }
            }
            """);
        warmupResponse.ShouldNotContain("errors");
        ((string)warmupResp["data"]!["generatePersonalAccessToken"]!["token"]!).ShouldNotBeNullOrEmpty();
        using (var scope = ScopedService.CreateScope())
        {
            var redis = scope.ServiceProvider.GetRequiredService<IRedisDatabase>();
            var cached = await redis.GetNamedAsync<UserSession>(UserSession.GetCacheKey(userId, LoginProviderEnum.Local));
            cached.ShouldNotBeNull();
            cached.UserId.ShouldBe(userId);
            cached.Token.ShouldBe(token);
            cached.SupplementaryClaims.ShouldNotBeEmpty();
        }

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

    [Fact]
    public async Task RefreshCache_ShouldKeepTrackedAndPersistedSessionUnchanged()
    {
        var (userId, username, passwordMd5) = await CreateTestUserAsync();
        var (login, loginResponse) = await AnonymousClient.PostGqlRequest(
            """
            mutation($userIdentifier: String!, $password: String!) {
                authenticate(request: { userIdentifier: $userIdentifier, password: $password }) {
                    token
                    userId
                }
            }
            """,
            new { userIdentifier = username, password = passwordMd5 });
        loginResponse.ShouldNotContain("errors");
        var token = (string)login["data"]!["authenticate"]!["token"]!;
        token.ShouldNotBeNullOrEmpty();

        BsonDocument before;
        string roleId;
        SupplementaryClaim[] cachedClaims;
        using (var scope = ScopedService.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var session = uow.Query<UserSession>()
                .First(x => x.UserId == userId && x.LoginProvider == LoginProviderEnum.Local);
            var originalClaims = session.SupplementaryClaims;
            var originalValues = originalClaims.ToArray();
            var originalUpdated = session.LastUpdatedOn;
            var originalModified = session.ModifiedOn;
            before = session.ToBsonDocument();
            var claims = new[] { new SupplementaryClaim("session-cache-regression", "refreshed") };

            await session.RefreshCacheAsync(claims, TimeSpan.FromMinutes(2));

            session.SupplementaryClaims.ShouldBeSameAs(originalClaims);
            session.SupplementaryClaims.ShouldBe(originalValues);
            session.Token.ShouldBe(token);
            session.LastUpdatedOn.ShouldBe(originalUpdated);
            session.ModifiedOn.ShouldBe(originalModified);
            session.ToBsonDocument().ShouldBe(before);
            var redis = scope.ServiceProvider.GetRequiredService<IRedisDatabase>();
            var cached = await redis.GetNamedAsync<UserSession>(session.CacheKey);
            cached.ShouldNotBeNull();
            cached.Id.ShouldBe(session.Id);
            cached.UserId.ShouldBe(userId);
            cached.LoginProvider.ShouldBe(LoginProviderEnum.Local);
            cached.Token.ShouldBe(token);
            cached.LastUpdatedOn.ShouldBe(originalUpdated);
            cached.ModifiedOn.ShouldBe(originalModified);
            cached.SupplementaryClaims.ShouldBe(claims);
            cachedClaims = cached.SupplementaryClaims.ToArray();

            var role = await uow.Request(new CreateRoleRequest
            {
                RoleCode = $"cache_{ObjectId.GenerateNewId()}",
                RoleName = "Session cache persistence regression",
                IsStatic = false,
                IsDefault = false
            });
            roleId = role.Id;
            await uow.SaveChanges();
            session.ToBsonDocument().ShouldBe(before);
        }

        using (var scope = ScopedService.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var persisted = uow.Query<UserSession>()
                .First(x => x.UserId == userId && x.LoginProvider == LoginProviderEnum.Local);
            persisted.ToBsonDocument().ShouldBe(before);
            uow.Query<IRole>().Any(x => x.Id == roleId).ShouldBeTrue();
            var root = Environment.GetEnvironmentVariable("PIMS_AF_EVIDENCE");
            if (!string.IsNullOrWhiteSpace(root))
            {
                var observation = new JsonObject
                {
                    ["userId"] = userId,
                    ["sessionId"] = persisted.Id,
                    ["roleId"] = roleId,
                    ["at"] = DateTimeOffset.UtcNow.ToString("o"),
                    ["persistedSessionUnchanged"] = persisted.ToBsonDocument().Equals(before),
                    ["beforeBsonSha256"] = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(before.ToBson())),
                    ["persistedBsonSha256"] = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(persisted.ToBsonDocument().ToBson())),
                    ["cachedClaims"] = System.Text.Json.JsonSerializer.SerializeToNode(cachedClaims)
                };
                await File.WriteAllTextAsync(Path.Combine(root, "session-cache-observation.json"), observation.ToJsonString());
            }
        }
    }

    private static HttpClient Record(HttpClient client)
    {
        var root = Environment.GetEnvironmentVariable("PIMS_AF_EVIDENCE");
        return string.IsNullOrWhiteSpace(root)
            ? client
            : new HttpClient(new SessionEvidenceHandler(client, root)) { BaseAddress = client.BaseAddress };
    }

    private sealed class SessionEvidenceHandler(HttpClient client, string root) : HttpMessageHandler
    {
        private static readonly object EvidenceLock = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            using var forwarded = new HttpRequestMessage(request.Method, request.RequestUri);
            foreach (var header in request.Headers) forwarded.Headers.TryAddWithoutValidation(header.Key, header.Value);
            var body = "{}";
            if (request.Content is not null)
            {
                var bytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
                body = System.Text.Encoding.UTF8.GetString(bytes);
                forwarded.Content = new ByteArrayContent(bytes);
                foreach (var header in request.Content.Headers) forwarded.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
            var response = await client.SendAsync(forwarded, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var entry = JsonNode.Parse(body)!.AsObject();
            entry["response"] = JsonNode.Parse(content);
            entry["statusCode"] = (int)response.StatusCode;
            entry["at"] = DateTimeOffset.UtcNow.ToString("o");
            Redact(entry);
            var folder = Path.Combine(root, "session-api");
            lock (EvidenceLock)
            {
                Directory.CreateDirectory(folder);
                File.AppendAllText(Path.Combine(folder, "http.jsonl"), entry.ToJsonString() + Environment.NewLine);
            }
            return response;
        }

        private static void Redact(JsonNode? node)
        {
            if (node is JsonObject obj)
            {
                foreach (var pair in obj.ToArray())
                {
                    if (pair.Key is "token" or "password" or "accessToken" or "refreshToken")
                        obj[pair.Key] = "[REDACTED]";
                    else
                        Redact(pair.Value);
                }
            }
            else if (node is JsonArray array)
            {
                foreach (var child in array) Redact(child);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) client.Dispose();
            base.Dispose(disposing);
        }
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
