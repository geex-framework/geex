using System.Security.Claims;
using Geex.Extensions.Authentication;
using Geex.Extensions.Authentication.Requests;
using Geex.Extensions.Identity;
using Geex.Extensions.Identity.Core.Entities;
using Geex.Extensions.Identity.Requests;
using Geex.Extensions.Requests.MultiTenant;
using Geex.MultiTenant;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using Shouldly;

namespace Geex.Tests.FeatureTests
{
    [Collection(nameof(TestsCollection))]
    public class AuthenticationLoginServiceTests : TestsBase
    {
        public AuthenticationLoginServiceTests(TestApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task ResolveLogin_WhenUnlinked_ShouldReturnUserLoginLinkToken()
        {
            var openId = $"openid_{ObjectId.GenerateNewId()}";
            var code = TestLoginProvider.RegisterIdentity(openId, "wx-user");
            var userCountBefore = 0;

            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                userCountBefore = uow.Query<User>().Count();
                var result = await uow.Request(new ResolveLoginRequest
                {
                    LoginProvider = TestLoginProviders.TestExternal,
                    Code = code,
                });

                result.IsLinked.ShouldBeFalse();
                result.UserLoginLinkToken.ShouldNotBeNullOrWhiteSpace();
                result.Session.ShouldBeNull();
                result.DisplayName.ShouldBe("wx-user");
                uow.Query<User>().Count().ShouldBe(userCountBefore);
            }
        }

        [Fact]
        public async Task ResolveLogin_WhenLinked_ShouldReturnSession()
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
                user.UpsertLogin(TestLoginProviders.TestExternal, openId, [new Claim("nickname", "Linked")]);
                await uow.SaveChanges();
                userId = user.Id;
            }

            var code = TestLoginProvider.RegisterIdentity(openId, "Linked");
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var result = await uow.Request(new ResolveLoginRequest
                {
                    LoginProvider = TestLoginProviders.TestExternal,
                    Code = code,
                });

                result.IsLinked.ShouldBeTrue();
                result.Session.ShouldNotBeNull();
                result.Session!.UserId.ShouldBe(userId);
                result.UserLoginLinkToken.ShouldBeNull();
            }
        }

        [Fact]
        public async Task LinkLogin_ShouldLinkAndIssueExternalSession()
        {
            var openId = $"openid_{ObjectId.GenerateNewId()}";
            var code = TestLoginProvider.RegisterIdentity(openId, "to-link");
            string UserLoginLinkToken;
            string userId;

            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var resolve = await uow.Request(new ResolveLoginRequest
                {
                    LoginProvider = TestLoginProviders.TestExternal,
                    Code = code,
                });
                UserLoginLinkToken = resolve.UserLoginLinkToken!;
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
                    var session = await uow.Request(new LinkLoginRequest
                    {
                        UserLoginLinkToken = UserLoginLinkToken,
                    });
                    session.UserId.ShouldBe(userId);
                    session.LoginProvider.ShouldBe(TestLoginProviders.TestExternal);
                }
            }

            var code2 = TestLoginProvider.RegisterIdentity(openId, "to-link");
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var result = await uow.Request(new ResolveLoginRequest
                {
                    LoginProvider = TestLoginProviders.TestExternal,
                    Code = code2,
                });
                result.IsLinked.ShouldBeTrue();
                result.Session!.UserId.ShouldBe(userId);
            }
        }

        [Fact]
        public async Task LinkLogin_WithoutLogin_ShouldFail()
        {
            var openId = $"openid_{ObjectId.GenerateNewId()}";
            var code = TestLoginProvider.RegisterIdentity(openId);
            string UserLoginLinkToken;
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var resolve = await uow.Request(new ResolveLoginRequest
                {
                    LoginProvider = TestLoginProviders.TestExternal,
                    Code = code,
                });
                UserLoginLinkToken = resolve.UserLoginLinkToken!;
            }

            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                await Should.ThrowAsync<BusinessException>(async () =>
                    await uow.Request(new LinkLoginRequest { UserLoginLinkToken = UserLoginLinkToken }));
            }
        }

        [Fact]
        public async Task LinkLogin_InvalidToken_ShouldFail()
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
                    await uow.Request(new LinkLoginRequest { UserLoginLinkToken = "invalid-token" }));
            }
        }

        [Fact]
        public async Task RegisterAndLinkLogin_ShouldCreateUserAndAllowResolve()
        {
            var openId = $"openid_{ObjectId.GenerateNewId()}";
            var code = TestLoginProvider.RegisterIdentity(openId, "new-wx");
            string UserLoginLinkToken;
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var resolve = await uow.Request(new ResolveLoginRequest
                {
                    LoginProvider = TestLoginProviders.TestExternal,
                    Code = code,
                });
                UserLoginLinkToken = resolve.UserLoginLinkToken!;
            }

            string userId;
            var username = $"reg_ext_{ObjectId.GenerateNewId()}";
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var session = await uow.Request(new RegisterAndLinkLoginRequest
                {
                    UserLoginLinkToken = UserLoginLinkToken,
                    Username = username,
                    Password = "Password123!".ToMd5(),
                    Email = $"{username}@test.com",
                    Nickname = "new-wx",
                });
                userId = session.UserId;
                session.LoginProvider.ShouldBe(TestLoginProviders.TestExternal);
            }

            var code2 = TestLoginProvider.RegisterIdentity(openId, "new-wx");
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var result = await uow.Request(new ResolveLoginRequest
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
            var code = TestLoginProvider.RegisterIdentity(openId);
            using var scope = ScopedService.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            await Should.ThrowAsync<BusinessException>(async () =>
                await uow.Request(new FederateAuthenticateRequest
                {
                    LoginProvider = TestLoginProviders.TestExternal,
                    Code = code,
                }));
        }

        [Fact]
        public async Task Authenticate_WhenUserInOtherTenant_ShouldFail()
        {
            var tenantA = $"tenant_a_{ObjectId.GenerateNewId()}";
            var tenantB = $"tenant_b_{ObjectId.GenerateNewId()}";
            var username = $"x_tenant_user_{ObjectId.GenerateNewId()}";
            var password = "Password123!".ToMd5();

            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                await uow.Request(new CreateTenantRequest { Code = tenantA, Name = "Tenant A" });
                await uow.Request(new CreateTenantRequest { Code = tenantB, Name = "Tenant B" });
                await uow.SaveChanges();
            }

            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var currentTenant = scope.ServiceProvider.GetRequiredService<ICurrentTenant>();
                using (currentTenant.Change(tenantA))
                {
                    await uow.Request(new CreateUserRequest
                    {
                        Username = username,
                        Email = $"{username}@test.com",
                        Password = password,
                        Nickname = "Tenant A User",
                        IsEnable = true,
                        RoleIds = [],
                        OrgCodes = [],
                    });
                    await uow.SaveChanges();
                }
            }

            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var currentTenant = scope.ServiceProvider.GetRequiredService<ICurrentTenant>();
                using (currentTenant.Change(tenantB))
                {
                    var ex = await Should.ThrowAsync<BusinessException>(async () =>
                        await uow.Request(new AuthenticateRequest
                        {
                            UserIdentifier = username,
                            Password = password,
                        }));
                    ex.ExceptionName.ShouldBe(GeexExceptionType.NotFound.Name);
                    ex.Message.ShouldContain("用户名或者密码不正确");
                }
            }
        }

        [Fact]
        public async Task Authenticate_WhenUserInSameTenant_ShouldSucceed()
        {
            var tenantA = $"tenant_a_{ObjectId.GenerateNewId()}";
            var username = $"same_tenant_user_{ObjectId.GenerateNewId()}";
            var password = "Password123!".ToMd5();

            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                await uow.Request(new CreateTenantRequest { Code = tenantA, Name = "Tenant A" });
                await uow.SaveChanges();
            }

            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var currentTenant = scope.ServiceProvider.GetRequiredService<ICurrentTenant>();
                using (currentTenant.Change(tenantA))
                {
                    await uow.Request(new CreateUserRequest
                    {
                        Username = username,
                        Email = $"{username}@test.com",
                        Password = password,
                        Nickname = "Same Tenant User",
                        IsEnable = true,
                        RoleIds = [],
                        OrgCodes = [],
                    });
                    await uow.SaveChanges();
                }
            }

            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var currentTenant = scope.ServiceProvider.GetRequiredService<ICurrentTenant>();
                using (currentTenant.Change(tenantA))
                {
                    var session = await uow.Request(new AuthenticateRequest
                    {
                        UserIdentifier = username,
                        Password = password,
                    });
                    session.Token.ShouldNotBeNullOrWhiteSpace();
                }
            }
        }

        [Fact]
        public async Task Authenticate_SuperAdmin_ShouldSucceedAcrossTenants()
        {
            var tenantB = $"tenant_b_{ObjectId.GenerateNewId()}";

            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                await uow.Request(new CreateTenantRequest { Code = tenantB, Name = "Tenant B" });
                await uow.SaveChanges();
            }

            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var currentTenant = scope.ServiceProvider.GetRequiredService<ICurrentTenant>();
                using (currentTenant.Change(tenantB))
                {
                    var session = await uow.Request(new AuthenticateRequest
                    {
                        UserIdentifier = GeexConstants.SuperAdminName,
                        Password = GeexConstants.SuperAdminName.ToMd5(),
                    });
                    session.Token.ShouldNotBeNullOrWhiteSpace();
                }
            }
        }
    }
}
