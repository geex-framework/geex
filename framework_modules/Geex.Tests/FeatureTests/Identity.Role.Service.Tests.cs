using Geex.Extensions.Identity;
using Geex.Extensions.Identity.Requests;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace Geex.Tests.FeatureTests
{
    [Collection(nameof(TestsCollection))]
    public class IdentityRoleServiceTests : TestsBase
    {
        public IdentityRoleServiceTests(TestApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task CreateRoleServiceShouldWork()
        {
            // Arrange
            var testRoleCode = $"testrole_{ObjectId.GenerateNewId()}";

            // Act
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetService<IUnitOfWork>();
                
                var request = new CreateRoleRequest
                {
                    RoleCode = testRoleCode,
                    RoleName = "Test Role",
                    IsStatic = false,
                    IsDefault = false
                };

                var role = await uow.Request(request);
                await uow.SaveChanges();

                // Assert
                role.ShouldNotBeNull();
                role.Code.ShouldBe(testRoleCode);
                role.Name.ShouldBe("Test Role");
                role.IsStatic.ShouldBe(false);
                role.IsDefault.ShouldBe(false);
                role.IsEnabled.ShouldBe(true);
            }
        }

        [Fact]
        public async Task CreateStaticRoleShouldWork()
        {
            // Arrange
            var testRoleCode = $"staticrole_{ObjectId.GenerateNewId()}";

            // Act
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetService<IUnitOfWork>();
                
                var request = new CreateRoleRequest
                {
                    RoleCode = testRoleCode,
                    RoleName = "Static Role",
                    IsStatic = true,
                    IsDefault = false
                };

                var role = await uow.Request(request);
                await uow.SaveChanges();

                // Assert
                role.IsStatic.ShouldBe(true);
            }
        }

        [Fact]
        public async Task CreateDefaultRoleShouldWork()
        {
            // Arrange
            var testRoleCode = $"defaultrole_{ObjectId.GenerateNewId()}";

            // Act
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetService<IUnitOfWork>();
                
                var request = new CreateRoleRequest
                {
                    RoleCode = testRoleCode,
                    RoleName = "Default Role",
                    IsStatic = false,
                    IsDefault = true
                };

                var role = await uow.Request(request);
                await uow.SaveChanges();

                // Assert
                role.IsDefault.ShouldBe(true);
            }
        }

        [Fact]
        public async Task SetRoleAsDefaultShouldWork()
        {
            // Arrange
            var testRoleCode = $"setdefault_{ObjectId.GenerateNewId()}";
            string roleId;

            // Create role
            using (var setupScope = ScopedService.CreateScope())
            {
                var setupUow = setupScope.ServiceProvider.GetService<IUnitOfWork>();
                
                var role = await setupUow.Request(new CreateRoleRequest
                {
                    RoleCode = testRoleCode,
                    RoleName = "Set Default Role",
                    IsStatic = false,
                    IsDefault = false
                });
                await setupUow.SaveChanges();
                roleId = role.Id;
            }

            // Act
            using (var setDefaultScope = ScopedService.CreateScope())
            {
                var setDefaultUow = setDefaultScope.ServiceProvider.GetService<IUnitOfWork>();
                
                var setDefaultRequest = new SetRoleDefaultRequest(roleId);
                await setDefaultUow.Request(setDefaultRequest);
                await setDefaultUow.SaveChanges();
            }

            // Assert
            using (var verifyScope = ScopedService.CreateScope())
            {
                var verifyUow = verifyScope.ServiceProvider.GetService<IUnitOfWork>();
                var updatedRole = verifyUow.Query<IRole>().First(x => x.Id == roleId);
                updatedRole.IsDefault.ShouldBe(true);
            }
        }

        [Fact]
        public async Task SetDefaultShouldUnsetPreviousDefaultRole()
        {
            // Arrange
            var role1Code = $"role1_{ObjectId.GenerateNewId()}";
            var role2Code = $"role2_{ObjectId.GenerateNewId()}";
            string role1Id, role2Id;

            // Create roles
            using (var setupScope = ScopedService.CreateScope())
            {
                var setupUow = setupScope.ServiceProvider.GetService<IUnitOfWork>();
                
                var role1 = await setupUow.Request(new CreateRoleRequest
                {
                    RoleCode = role1Code,
                    RoleName = $"Role 1 {ObjectId.GenerateNewId()}",
                    IsStatic = false,
                    IsDefault = true
                });

                var role2 = await setupUow.Request(new CreateRoleRequest
                {
                    RoleCode = role2Code,
                    RoleName = $"Role 2 {ObjectId.GenerateNewId()}",
                    IsStatic = false,
                    IsDefault = false
                });
                await setupUow.SaveChanges();
                
                role1Id = role1.Id;
                role2Id = role2.Id;
            }

            // Act
            using (var setDefaultScope = ScopedService.CreateScope())
            {
                var setDefaultUow = setDefaultScope.ServiceProvider.GetService<IUnitOfWork>();
                
                var setDefaultRequest = new SetRoleDefaultRequest(role2Id);
                await setDefaultUow.Request(setDefaultRequest);
                await setDefaultUow.SaveChanges();
            }

            // Assert
            using (var verifyScope = ScopedService.CreateScope())
            {
                var verifyUow = verifyScope.ServiceProvider.GetService<IUnitOfWork>();
                var updatedRole1 = verifyUow.Query<IRole>().First(x => x.Id == role1Id);
                var updatedRole2 = verifyUow.Query<IRole>().First(x => x.Id == role2Id);

                updatedRole1.IsDefault.ShouldBe(false);
                updatedRole2.IsDefault.ShouldBe(true);
            }
        }

        [Fact]
        public async Task QueryRolesShouldWork()
        {
            // Arrange & Act
            using (var setupScope = ScopedService.CreateScope())
            {
                var setupUow = setupScope.ServiceProvider.GetService<IUnitOfWork>();
                
                await setupUow.Request(new CreateRoleRequest
                {
                    RoleCode = $"query1_{ObjectId.GenerateNewId()}",
                    RoleName = "Query Role 1",
                    IsStatic = false,
                    IsDefault = false
                });

                await setupUow.Request(new CreateRoleRequest
                {
                    RoleCode = $"query2_{ObjectId.GenerateNewId()}",
                    RoleName = "Query Role 2",
                    IsStatic = false,
                    IsDefault = false
                });
                await setupUow.SaveChanges();

                var roles = setupUow.Query<IRole>().ToList();

                // Assert
                roles.Count.ShouldBeGreaterThanOrEqualTo(2);
            }
        }

        [Fact]
        public async Task RoleValidationShouldWork()
        {
            // Arrange
            var duplicateCode = $"duplicate_{ObjectId.GenerateNewId()}";

            // Create first role
            using (var setupScope = ScopedService.CreateScope())
            {
                var setupUow = setupScope.ServiceProvider.GetService<IUnitOfWork>();
                
                await setupUow.Request(new CreateRoleRequest
                {
                    RoleCode = duplicateCode,
                    RoleName = "First Role",
                    IsStatic = false,
                    IsDefault = false
                });
                await setupUow.SaveChanges();
            }

            // Act & Assert - Try to create duplicate
            using (var duplicateScope = ScopedService.CreateScope())
            {
                var duplicateUow = duplicateScope.ServiceProvider.GetService<IUnitOfWork>();
                
                Should.Throw<Exception>(async () =>
                {
                    await duplicateUow.Request(new CreateRoleRequest
                    {
                        RoleCode = duplicateCode,
                        RoleName = "Duplicate Role",
                        IsStatic = false,
                        IsDefault = false
                    });
                    await duplicateUow.SaveChanges();
                });
            }
        }
    }
}
