using System.Security.Claims;
using Geex.Extensions.Authentication;
using Geex.Extensions.Authentication.Requests;
using Geex.Extensions.Identity;
using Geex.Extensions.Identity.Core.Entities;
using Geex.Extensions.Identity.Requests;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using Shouldly;

namespace Geex.Tests.FeatureTests
{
    [Collection(nameof(TestsCollection))]
    public class AuthenticationExternalLoginServiceTests : TestsBase
    {
        public AuthenticationExternalLoginServiceTests(TestApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task ResolveExternalLogin_WhenUnlinked_ShouldReturnAccountLinkToken()
        {
            var openId = $"openid_{ObjectId.GenerateNewId()}";
            var code = TestExternalLoginProvider.RegisterIdentity(openId, "wx-user");
            var userCountBefore = 0;

            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                userCountBefore = uow.Query<User>().Count();
                var result = await uow.Request(new ResolveExternalLoginRequest
                {
                    LoginProvider = TestLoginProviders.TestExternal,
                    Code = code,
                });

                result.IsLinked.ShouldBeFalse();
                result.AccountLinkToken.ShouldNotBeNullOrWhiteSpace();
                result.Session.ShouldBeNull();
                result.DisplayName.ShouldBe("wx-user");
                uow.Query<User>().Count().ShouldBe(userCountBefore);
            }
        }

        [Fact]
        public async Task ResolveExternalLogin_WhenLinked_ShouldReturnSession()
        {
            var openId = $"openid_{ObjectId.GenerateNewId()}";
            string userId;
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var username = $"ext_user_{ObjectId.GenerateNewId()}";
                var user = await uow.Request(new CreateUserRequest
                {
                    Username = username,
                    Email = $"{username}@test.com",
                    Password = "Password123!".ToMd5(),
                    Nickname = "Linked",
                    IsEnable = true,
                    RoleIds = [],
                    OrgCodes = [],
                });
                user.UpsertExternalLogin(TestLoginProviders.TestExternal, openId, [new Claim("nickname", "Linked")], uow);
                await uow.SaveChanges();
                userId = user.Id;
            }

            var code = TestExternalLoginProvider.RegisterIdentity(openId, "Linked");
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var result = await uow.Request(new ResolveExternalLoginRequest
                {
                    LoginProvider = TestLoginProviders.TestExternal,
                    Code = code,
                });

                result.IsLinked.ShouldBeTrue();
                result.Session.ShouldNotBeNull();
                result.Session!.UserId.ShouldBe(userId);
                result.AccountLinkToken.ShouldBeNull();
            }
        }

        [Fact]
        public async Task LinkExternalLogin_ShouldLinkAndIssueExternalSession()
        {
            var openId = $"openid_{ObjectId.GenerateNewId()}";
            var code = TestExternalLoginProvider.RegisterIdentity(openId, "to-link");
            string accountLinkToken;
            string userId;

            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var resolve = await uow.Request(new ResolveExternalLoginRequest
                {
                    LoginProvider = TestLoginProviders.TestExternal,
                    Code = code,
                });
                accountLinkToken = resolve.AccountLinkToken!;
            }

            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var username = $"link_user_{ObjectId.GenerateNewId()}";
                var user = await uow.Request(new CreateUserRequest
                {
                    Username = username,
                    Email = $"{username}@test.com",
                    Password = "Password123!".ToMd5(),
                    Nickname = "Local",
                    IsEnable = true,
                    RoleIds = [],
                    OrgCodes = [],
                });
                await uow.SaveChanges();
                userId = user.Id;

                var currentUser = scope.ServiceProvider.GetRequiredService<ICurrentUser>();
                using (currentUser.Change(userId))
                {
                    var session = await uow.Request(new LinkExternalLoginRequest
                    {
                        AccountLinkToken = accountLinkToken,
                    });
                    session.UserId.ShouldBe(userId);
                    session.LoginProvider.ShouldBe(TestLoginProviders.TestExternal);
                }
            }

            var code2 = TestExternalLoginProvider.RegisterIdentity(openId, "to-link");
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var result = await uow.Request(new ResolveExternalLoginRequest
                {
                    LoginProvider = TestLoginProviders.TestExternal,
                    Code = code2,
                });
                result.IsLinked.ShouldBeTrue();
                result.Session!.UserId.ShouldBe(userId);
            }
        }

        [Fact]
        public async Task LinkExternalLogin_WithoutLogin_ShouldFail()
        {
            var openId = $"openid_{ObjectId.GenerateNewId()}";
            var code = TestExternalLoginProvider.RegisterIdentity(openId);
            string accountLinkToken;
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var resolve = await uow.Request(new ResolveExternalLoginRequest
                {
                    LoginProvider = TestLoginProviders.TestExternal,
                    Code = code,
                });
                accountLinkToken = resolve.AccountLinkToken!;
            }

            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                await Should.ThrowAsync<BusinessException>(async () =>
                    await uow.Request(new LinkExternalLoginRequest { AccountLinkToken = accountLinkToken }));
            }
        }

        [Fact]
        public async Task LinkExternalLogin_InvalidToken_ShouldFail()
        {
            using var scope = ScopedService.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var username = $"link_user_{ObjectId.GenerateNewId()}";
            var user = await uow.Request(new CreateUserRequest
            {
                Username = username,
                Email = $"{username}@test.com",
                Password = "Password123!".ToMd5(),
                IsEnable = true,
                RoleIds = [],
                OrgCodes = [],
            });
            await uow.SaveChanges();
            var currentUser = scope.ServiceProvider.GetRequiredService<ICurrentUser>();
            using (currentUser.Change(user.Id))
            {
                await Should.ThrowAsync<BusinessException>(async () =>
                    await uow.Request(new LinkExternalLoginRequest { AccountLinkToken = "invalid-token" }));
            }
        }

        [Fact]
        public async Task RegisterAndLinkExternalLogin_ShouldCreateUserAndAllowResolve()
        {
            var openId = $"openid_{ObjectId.GenerateNewId()}";
            var code = TestExternalLoginProvider.RegisterIdentity(openId, "new-wx");
            string accountLinkToken;
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var resolve = await uow.Request(new ResolveExternalLoginRequest
                {
                    LoginProvider = TestLoginProviders.TestExternal,
                    Code = code,
                });
                accountLinkToken = resolve.AccountLinkToken!;
            }

            string userId;
            var username = $"reg_ext_{ObjectId.GenerateNewId()}";
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var session = await uow.Request(new RegisterAndLinkExternalLoginRequest
                {
                    AccountLinkToken = accountLinkToken,
                    Username = username,
                    Password = "Password123!".ToMd5(),
                    Email = $"{username}@test.com",
                    Nickname = "new-wx",
                });
                userId = session.UserId;
                session.LoginProvider.ShouldBe(TestLoginProviders.TestExternal);
            }

            var code2 = TestExternalLoginProvider.RegisterIdentity(openId, "new-wx");
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var result = await uow.Request(new ResolveExternalLoginRequest
                {
                    LoginProvider = TestLoginProviders.TestExternal,
                    Code = code2,
                });
                result.IsLinked.ShouldBeTrue();
                result.Session!.UserId.ShouldBe(userId);
            }
        }

        [Fact]
        public async Task FederateAuthenticate_WhenUnlinked_ShouldFail()
        {
            var openId = $"openid_{ObjectId.GenerateNewId()}";
            var code = TestExternalLoginProvider.RegisterIdentity(openId);
            using var scope = ScopedService.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            await Should.ThrowAsync<BusinessException>(async () =>
                await uow.Request(new FederateAuthenticateRequest
                {
                    LoginProvider = TestLoginProviders.TestExternal,
                    Code = code,
                }));
        }
    }
}
