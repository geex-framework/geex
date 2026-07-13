using System.Security.Claims;
using Geex.Extensions.Authentication;
using Geex.Extensions.Authentication.Requests;
using Geex.Extensions.Authentication.Wechat;
using Geex.Extensions.Identity;
using Geex.Extensions.Identity.Core.Entities;
using Geex.Extensions.Identity.Requests;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using Shouldly;

namespace Geex.Tests.FeatureTests
{
    [Collection(nameof(TestsCollection))]
    public class AuthenticationWechatServiceTests : TestsBase
    {
        public AuthenticationWechatServiceTests(TestApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task WechatWeb_Resolve_WhenUnlinked_ShouldReturnAccountLinkToken()
        {
            var openId = $"wx_openid_{ObjectId.GenerateNewId()}";
            var code = FakeWechatApiClient.RegisterWeb(openId);
            var userCountBefore = 0;

            using var scope = ScopedService.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            userCountBefore = uow.Query<User>().Count();
            var result = await uow.Request(new ResolveExternalLoginRequest
            {
                LoginProvider = WechatLoginProviders.WechatWeb,
                Code = code,
            });

            result.IsLinked.ShouldBeFalse();
            result.AccountLinkToken.ShouldNotBeNullOrWhiteSpace();
            result.DisplayName.ShouldBe($"wx_{openId}");
            uow.Query<User>().Count().ShouldBe(userCountBefore);
        }

        [Fact]
        public async Task WechatMiniProgram_Resolve_WhenUnlinked_ShouldReturnAccountLinkToken()
        {
            var openId = $"mp_openid_{ObjectId.GenerateNewId()}";
            var code = FakeWechatApiClient.RegisterMiniProgram(openId);

            using var scope = ScopedService.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var result = await uow.Request(new ResolveExternalLoginRequest
            {
                LoginProvider = WechatLoginProviders.WechatMiniProgram,
                Code = code,
            });

            result.IsLinked.ShouldBeFalse();
            result.AccountLinkToken.ShouldNotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task WechatWeb_Resolve_WhenLinked_ShouldReturnSession()
        {
            var openId = $"wx_openid_{ObjectId.GenerateNewId()}";
            string userId;
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var username = $"wx_user_{ObjectId.GenerateNewId()}";
                var user = await uow.Request(new CreateUserRequest
                {
                    Username = username,
                    Email = $"{username}@test.com",
                    Password = "Password123!".ToMd5(),
                    IsEnable = true,
                    RoleIds = [],
                    OrgCodes = [],
                });
                user.UpsertExternalLogin(WechatLoginProviders.WechatWeb, openId, [new Claim("openid", openId)], uow);
                await uow.SaveChanges();
                userId = user.Id;
            }

            var code = FakeWechatApiClient.RegisterWeb(openId);
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var result = await uow.Request(new ResolveExternalLoginRequest
                {
                    LoginProvider = WechatLoginProviders.WechatWeb,
                    Code = code,
                });
                result.IsLinked.ShouldBeTrue();
                result.Session!.UserId.ShouldBe(userId);
            }
        }

        [Fact]
        public async Task WechatWeb_LinkFlow_ShouldSmoke()
        {
            var openId = $"wx_openid_{ObjectId.GenerateNewId()}";
            var code = FakeWechatApiClient.RegisterWeb(openId);
            string accountLinkToken;
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var resolve = await uow.Request(new ResolveExternalLoginRequest
                {
                    LoginProvider = WechatLoginProviders.WechatWeb,
                    Code = code,
                });
                accountLinkToken = resolve.AccountLinkToken!;
            }

            string userId;
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var username = $"wx_link_{ObjectId.GenerateNewId()}";
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
                userId = user.Id;
                var currentUser = scope.ServiceProvider.GetRequiredService<ICurrentUser>();
                using (currentUser.Change(userId))
                {
                    var session = await uow.Request(new LinkExternalLoginRequest
                    {
                        AccountLinkToken = accountLinkToken,
                    });
                    session.LoginProvider.ShouldBe(WechatLoginProviders.WechatWeb);
                }
            }

            var code2 = FakeWechatApiClient.RegisterWeb(openId);
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var result = await uow.Request(new ResolveExternalLoginRequest
                {
                    LoginProvider = WechatLoginProviders.WechatWeb,
                    Code = code2,
                });
                result.IsLinked.ShouldBeTrue();
                result.Session!.UserId.ShouldBe(userId);
            }
        }
    }
}
