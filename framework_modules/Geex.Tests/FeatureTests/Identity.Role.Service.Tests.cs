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
    public class IdentityRoleServiceTests
    {
        private readonly TestApplicationFactory _factory;

        public IdentityRoleServiceTests(TestApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task CreateRoleServiceShouldWork()
        {
            // Arrange
            var service = _factory.Services;
            var uow = service.GetService<IUnitOfWork>();
            var testRoleCode = $"testrole_{ObjectId.GenerateNewId()}";

            var request = new CreateRoleRequest
            {
                RoleCode = testRoleCode,
                RoleName = "Test Role",
                IsStatic = false,
                IsDefault = false
            };

            // Act
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

        [Fact]
        public async Task CreateStaticRoleShouldWork()
        {
            // Arrange
            var service = _factory.Services;
            var uow = service.GetService<IUnitOfWork>();
            var testRoleCode = $"staticrole_{ObjectId.GenerateNewId()}";

            var request = new CreateRoleRequest
            {
                RoleCode = testRoleCode,
                RoleName = "Static Role",
                IsStatic = true,
                IsDefault = false
            };

            // Act
            var role = await uow.Request(request);
            await uow.SaveChanges();

            // Assert
            role.IsStatic.ShouldBe(true);
        }

        [Fact]
        public async Task CreateDefaultRoleShouldWork()
        {
            // Arrange
            var service = _factory.Services;
            var uow = service.GetService<IUnitOfWork>();
            var testRoleCode = $"defaultrole_{ObjectId.GenerateNewId()}";

            var request = new CreateRoleRequest
            {
                RoleCode = testRoleCode,
                RoleName = "Default Role",
                IsStatic = false,
                IsDefault = true
            };

            // Act
            var role = await uow.Request(request);
            await uow.SaveChanges();

            // Assert
            role.IsDefault.ShouldBe(true);
        }

        [Fact]
        public async Task SetRoleAsDefaultShouldWork()
        {
            // Arrange
            var service = _factory.Services;
            var uow = service.GetService<IUnitOfWork>();
            var testRoleCode = $"setdefault_{ObjectId.GenerateNewId()}";

            var role = await uow.Request(new CreateRoleRequest
            {
                RoleCode = testRoleCode,
                RoleName = "Set Default Role",
                IsStatic = false,
                IsDefault = false
            });
            await uow.SaveChanges();

            var setDefaultRequest = new SetRoleDefaultRequest(role.Id);

            // Act
            await uow.Request(setDefaultRequest);
            await uow.SaveChanges();

            // Assert
            using var verifyService = service.CreateScope();
            var verifyUow = verifyService.ServiceProvider.GetService<IUnitOfWork>();
            var updatedRole = verifyUow.Query<IRole>().First(x => x.Id == role.Id);
            updatedRole.IsDefault.ShouldBe(true);
        }

        [Fact]
        public async Task SetDefaultShouldUnsetPreviousDefaultRole()
        {
            // Arrange
            var service = _factory.Services;
            var uow = service.GetService<IUnitOfWork>();

            var role1 = await uow.Request(new CreateRoleRequest
            {
                RoleCode = $"role1_{ObjectId.GenerateNewId()}",
                RoleName = "Role 1",
                IsStatic = false,
                IsDefault = true
            });

            var role2 = await uow.Request(new CreateRoleRequest
            {
                RoleCode = $"role2_{ObjectId.GenerateNewId()}",
                RoleName = "Role 2",
                IsStatic = false,
                IsDefault = false
            });
            await uow.SaveChanges();

            var setDefaultRequest = new SetRoleDefaultRequest(role2.Id);

            // Act
            await uow.Request(setDefaultRequest);
            await uow.SaveChanges();

            // Assert
            using var verifyService = service.CreateScope();
            var verifyUow = verifyService.ServiceProvider.GetService<IUnitOfWork>();
            var updatedRole1 = verifyUow.Query<IRole>().First(x => x.Id == role1.Id);
            var updatedRole2 = verifyUow.Query<IRole>().First(x => x.Id == role2.Id);

            updatedRole1.IsDefault.ShouldBe(false);
            updatedRole2.IsDefault.ShouldBe(true);
        }

        [Fact]
        public async Task QueryRolesShouldWork()
        {
            // Arrange
            var service = _factory.Services;
            var uow = service.GetService<IUnitOfWork>();

            await uow.Request(new CreateRoleRequest
            {
                RoleCode = $"query1_{ObjectId.GenerateNewId()}",
                RoleName = "Query Role 1",
                IsStatic = false,
                IsDefault = false
            });

            await uow.Request(new CreateRoleRequest
            {
                RoleCode = $"query2_{ObjectId.GenerateNewId()}",
                RoleName = "Query Role 2",
                IsStatic = false,
                IsDefault = false
            });
            await uow.SaveChanges();

            // Act
            var roles = uow.Query<IRole>().ToList();

            // Assert
            roles.Count.ShouldBeGreaterThanOrEqualTo(2);
        }

        [Fact]
        public async Task RoleValidationShouldWork()
        {
            // Arrange
            var service = _factory.Services;
            var uow = service.GetService<IUnitOfWork>();
            var duplicateCode = $"duplicate_{ObjectId.GenerateNewId()}";

            await uow.Request(new CreateRoleRequest
            {
                RoleCode = duplicateCode,
                RoleName = "First Role",
                IsStatic = false,
                IsDefault = false
            });
            await uow.SaveChanges();

            // Act & Assert
            Should.Throw<Exception>(async () =>
            {
                await uow.Request(new CreateRoleRequest
                {
                    RoleCode = duplicateCode,
                    RoleName = "Duplicate Role",
                    IsStatic = false,
                    IsDefault = false
                });
                await uow.SaveChanges();
            });
        }
    }
}
