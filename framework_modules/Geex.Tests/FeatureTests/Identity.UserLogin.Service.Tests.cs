using System.Security.Claims;
using Geex.Abstractions;
using Geex.Extensions.Identity;
using Geex.Extensions.Identity.Core.Entities;
using Geex.Extensions.Identity.Requests;
using Geex.Extensions.MultiTenant.Core.Aggregates.Tenants;
using Geex.Extensions.Requests.MultiTenant;
using Geex.MultiTenant;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using Shouldly;

namespace Geex.Tests.FeatureTests
{
    [Collection(nameof(TestsCollection))]
    public class IdentityUserLoginServiceTests : TestsBase
    {
        public IdentityUserLoginServiceTests(TestApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task SameLogin_CanBindAcrossTenants()
        {
            var provider = LoginProviderEnum.FromValue($"TestWechat_{ObjectId.GenerateNewId()}");
            var openId = $"openid_{ObjectId.GenerateNewId()}";
            var tenantA = $"tenant_a_{ObjectId.GenerateNewId()}";
            var tenantB = $"tenant_b_{ObjectId.GenerateNewId()}";
            string? userIdA = null;
            string? userIdB = null;

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
                    var username = $"user_a_{ObjectId.GenerateNewId()}";
                    var user = await uow.Request(new CreateUserRequest
                    {
                        Username = username,
                        Email = $"{username}@test.com",
                        Password = "Password123!".ToMd5(),
                        Nickname = "User A",
                        IsEnable = true,
                        RoleIds = [],
                        OrgCodes = []
                    });
                    user.UpsertLogin(provider, openId, [new Claim("nickname", "wx-a")]);
                    await uow.SaveChanges();
                    userIdA = user.Id;
                    user.TenantCode.ShouldBe(tenantA);
                    user.Logins.First().TenantCode.ShouldBe(tenantA);
                }
            }

            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var currentTenant = scope.ServiceProvider.GetRequiredService<ICurrentTenant>();
                using (currentTenant.Change(tenantB))
                {
                    var username = $"user_b_{ObjectId.GenerateNewId()}";
                    var user = await uow.Request(new CreateUserRequest
                    {
                        Username = username,
                        Email = $"{username}@test.com",
                        Password = "Password123!".ToMd5(),
                        Nickname = "User B",
                        IsEnable = true,
                        RoleIds = [],
                        OrgCodes = []
                    });
                    user.UpsertLogin(provider, openId, [new Claim("nickname", "wx-b")]);
                    await uow.SaveChanges();
                    userIdB = user.Id;
                    user.TenantCode.ShouldBe(tenantB);
                    user.Logins.First().TenantCode.ShouldBe(tenantB);
                }
            }

            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var currentTenant = scope.ServiceProvider.GetRequiredService<ICurrentTenant>();
                using (currentTenant.Change(tenantA))
                {
                    var found = uow.Query<User>().FindByLogin(provider, openId);
                    found.ShouldNotBeNull();
                    found.Id.ShouldBe(userIdA);
                }

                using (currentTenant.Change(tenantB))
                {
                    var found = uow.Query<User>().FindByLogin(provider, openId);
                    found.ShouldNotBeNull();
                    found.Id.ShouldBe(userIdB);
                }
            }

            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                await uow.DbContext.Query<Tenant>().Where(x => x.Code == tenantA || x.Code == tenantB).DeleteAsync();
            }
        }

        [Fact]
        public async Task SameLogin_CannotBindToDifferentUserInSameTenant()
        {
            var provider = LoginProviderEnum.FromValue($"TestWechat_{ObjectId.GenerateNewId()}");
            var openId = $"openid_{ObjectId.GenerateNewId()}";
            var tenantCode = $"tenant_{ObjectId.GenerateNewId()}";

            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                await uow.Request(new CreateTenantRequest { Code = tenantCode, Name = "Tenant" });
                await uow.SaveChanges();
            }

            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var currentTenant = scope.ServiceProvider.GetRequiredService<ICurrentTenant>();
                using (currentTenant.Change(tenantCode))
                {
                    var username1 = $"user1_{ObjectId.GenerateNewId()}";
                    var user1 = await uow.Request(new CreateUserRequest
                    {
                        Username = username1,
                        Email = $"{username1}@test.com",
                        Password = "Password123!".ToMd5(),
                        Nickname = "User 1",
                        IsEnable = true,
                        RoleIds = [],
                        OrgCodes = []
                    });
                    user1.UpsertLogin(provider, openId);
                    await uow.SaveChanges();

                    var username2 = $"user2_{ObjectId.GenerateNewId()}";
                    var user2 = await uow.Request(new CreateUserRequest
                    {
                        Username = username2,
                        Email = $"{username2}@test.com",
                        Password = "Password123!".ToMd5(),
                        Nickname = "User 2",
                        IsEnable = true,
                        RoleIds = [],
                        OrgCodes = []
                    });

                    Should.Throw<BusinessException>(() => user2.UpsertLogin(provider, openId));
                }
            }

            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                await uow.DbContext.Query<Tenant>().Where(x => x.Code == tenantCode).DeleteAsync();
            }
        }
    }
}
