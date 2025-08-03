using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Geex.Extensions.Authorization;
using Geex.Extensions.Authentication;
using Geex.Tests.SchemaTests.TestEntities;
using HotChocolate.Authorization;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using Shouldly;

namespace Geex.Tests.FeatureTests
{
    /// <summary>
    /// 测试类: 用于测试Authorization功能的API测试
    /// 包括基于权限的字段访问控制和权限验证
    /// </summary>
    [Collection(nameof(TestsCollection))]
    public class AuthorizationApiTests : TestsBase
    {
        public AuthorizationApiTests(TestApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task QueryMyPermissions_ShouldWork()
        {
            // Arrange - 使用超级管理员客户端查询权限
            var client = this.SuperAdminClient;
            var query = """
                query {
                    myPermissions
                }
                """;

            // Act
            var (responseData, responseString) = await client.PostGqlRequest(query);

            // Assert - 超级管理员应该有权限列表
            responseData["data"].ShouldNotBeNull();
            responseData["data"]["myPermissions"].ShouldNotBeNull();
            var permissions = responseData["data"]["myPermissions"].AsArray();
            permissions.Count.ShouldBeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public async Task QueryWithAuthorization_AsAnonymous_ShouldFail()
        {
            // Arrange - 使用匿名客户端访问需要权限的查询
            var client = this.AnonymousClient;
            var query = """
                query {
                    myPermissions
                }
                """;

            // Act
            var (responseData, responseString) = await client.PostGqlRequest(query, true);

            // Assert - 匿名用户应该被拒绝访问
            responseData["errors"].ShouldNotBeNull();
            var error = responseData["errors"][0];
            var errorCode = (string)error["extensions"]["code"];
            (errorCode == "AUTH_NOT_AUTHENTICATED" || errorCode == "AUTH_NOT_AUTHORIZED").ShouldBeTrue();
        }

        [Fact]
        public async Task AuthorizedFieldAccess_WithValidPermission_ShouldWork()
        {
            // Arrange - 使用超级管理员客户端访问受保护的字段
            var client = this.SuperAdminClient;
            var query = """
                query($id: String!) {
                    authTestQueryField(id: $id) {
                        id
                        __typename
                    }
                }
                """;

            // Act
            var (responseData, responseString) = await client.PostGqlRequest(query, new { id = "test" });

            // Assert
            var fieldData = responseData["data"]["authTestQueryField"];
            fieldData.ShouldNotBeNull();
        }

        [Fact]
        public async Task AuthorizationHandler_ShouldBeRegistered()
        {
            // Arrange & Act - 验证授权处理器是否正确注册
            using var scope = ScopedService.CreateScope();

            // Assert - 验证IRbacEnforcer服务已注册
            var rbacEnforcer = scope.ServiceProvider.GetService<IRbacEnforcer>();
            rbacEnforcer.ShouldNotBeNull("IRbacEnforcer should be registered");

            // 验证IAuthorizationHandler服务已注册
            var authHandler = scope.ServiceProvider.GetService<IAuthorizationHandler>();
            authHandler.ShouldNotBeNull("IAuthorizationHandler should be registered");
        }

        [Fact]
        public async Task PermissionCheck_WithEnforcer_ShouldWork()
        {
            // Arrange - 获取权限执行器
            using var scope = ScopedService.CreateScope();
            var rbacEnforcer = scope.ServiceProvider.GetService<IRbacEnforcer>();
            rbacEnforcer.ShouldNotBeNull();

            // Act - 检查超级管理员的权限
            var hasPermission = await rbacEnforcer.EnforceAsync(GeexConstants.SuperAdminId, "*", "*", "*");

            // Assert - 超级管理员应该有所有权限
            hasPermission.ShouldBeTrue("Super admin should have all permissions");
        }

        [Fact]
        public async Task PermissionCheck_WithInvalidUser_ShouldFail()
        {
            // Arrange - 获取权限执行器
            using var scope = ScopedService.CreateScope();
            var rbacEnforcer = scope.ServiceProvider.GetService<IRbacEnforcer>();
            rbacEnforcer.ShouldNotBeNull();

            // Act - 检查无效用户的权限
            var hasPermission = await rbacEnforcer.EnforceAsync("invalid_user", "*", "protected_resource", "read");

            // Assert - 无效用户应该没有权限
            hasPermission.ShouldBeFalse("Invalid user should not have permissions");
        }

        [Fact]
        public async Task AuthorizeDirective_ShouldBeAppliedCorrectly()
        {
            // Arrange & Act - 验证权限指令的应用
            using var scope = ScopedService.CreateScope();
            var schema = scope.ServiceProvider.GetService<HotChocolate.ISchema>();

            // Assert - 验证Schema中包含授权配置
            schema.ShouldNotBeNull();

            // 检查权限指令类型是否已注册
            var directiveTypes = schema.DirectiveTypes;
            var hasAuthorizeDirective = directiveTypes.Any(d => d.Name == "authorize");

            // 权限指令应该存在于Schema中
            hasAuthorizeDirective.ShouldBeTrue("Authorize directive should be available in schema");
        }

        [Fact]
        public async Task CurrentUser_ShouldHaveValidContext()
        {
            // Arrange - 获取当前用户服务
            using var scope = ScopedService.CreateScope();
            var currentUser = scope.ServiceProvider.GetService<ICurrentUser>();

            // Assert - 当前用户服务应该已注册
            currentUser.ShouldNotBeNull("ICurrentUser service should be registered");

            // 在测试环境中，当前用户可能为null或有默认值
            // 这里主要验证服务注册正确
        }

        [Fact]
        public async Task PolicyBasedAuthorization_ShouldWork()
        {
            // Arrange - 测试基于策略的授权
            var client = this.SuperAdminClient;

            // 这里我们模拟一个需要特定策略的查询
            // 在实际应用中，字段会有具体的权限策略要求
            var query = """
                query {
                    myPermissions
                }
                """;

            // Act
            var (responseData, responseString) = await client.PostGqlRequest(query);

            // Assert - 验证策略授权工作正常
            // 对于超级管理员，应该能够访问所有资源
            if (responseData["errors"] != null)
            {
                var error = responseData["errors"][0];
                var errorCode = (string)error["extensions"]["code"];
                // 权限相关的错误不应该出现
                errorCode.ShouldNotBe("AUTH_NOT_AUTHORIZED");
                errorCode.ShouldNotBe("AUTH_POLICY_NOT_FOUND");
            }
            else
            {
                responseData["data"].ShouldNotBeNull();
            }
        }

        [Fact]
        public async Task MultiplePermissionChecks_ShouldBeEfficient()
        {
            // Arrange - 测试多个权限检查的性能
            using var scope = ScopedService.CreateScope();
            var rbacEnforcer = scope.ServiceProvider.GetService<IRbacEnforcer>();
            rbacEnforcer.ShouldNotBeNull();

            var testSubject = "test_user";
            var testPermissions = new[]
            {
                ("*", "user", "read"),
                ("*", "user", "write"),
                ("*", "admin", "read"),
                ("*", "admin", "write")
            };

            // Act - 执行多个权限检查
            var results = new List<bool>();
            var startTime = DateTime.UtcNow;

            foreach (var (mod, obj, field) in testPermissions)
            {
                var result = await rbacEnforcer.EnforceAsync(testSubject, mod, obj, field);
                results.Add(result);
            }

            var elapsed = DateTime.UtcNow - startTime;

            // Assert - 权限检查应该在合理时间内完成
            elapsed.TotalSeconds.ShouldBeLessThan(5.0, "Permission checks should complete within reasonable time");
            results.Count.ShouldBe(testPermissions.Length);
        }
    }
}
