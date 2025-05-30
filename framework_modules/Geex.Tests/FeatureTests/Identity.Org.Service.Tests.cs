using Geex.Extensions.Identity;
using Geex.Extensions.Identity.Core.Entities;
using Geex.Extensions.Identity.Requests;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace Geex.Tests.FeatureTests
{
    [Collection(nameof(TestsCollection))]
    public class IdentityOrgServiceTests
    {
        private readonly TestApplicationFactory _factory;

        public IdentityOrgServiceTests(TestApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task CreateOrgServiceShouldWork()
        {
            // Arrange
            var service = _factory.Services;
            var uow = service.GetService<IUnitOfWork>();
            var testOrgCode = $"testorg_{ObjectId.GenerateNewId()}";

            var request = new CreateOrgRequest
            {
                Code = testOrgCode,
                Name = "Test Organization",
                OrgType = OrgTypeEnum.Default
            };

            // Act
            var org = await uow.Request(request);
            await uow.SaveChanges();

            // Assert
            org.ShouldNotBeNull();
            org.Code.ShouldBe(testOrgCode);
            org.Name.ShouldBe("Test Organization");
            org.OrgType.ShouldBe(OrgTypeEnum.Default);
        }

        [Fact]
        public async Task CreateSubOrganizationShouldWork()
        {
            // Arrange
            var service = _factory.Services;
            var uow = service.GetService<IUnitOfWork>();
            var parentOrgCode = $"parent_{ObjectId.GenerateNewId()}";
            var parentOrg = await CreateTestOrg(uow, parentOrgCode);
            
            var subOrgCode = $"{parentOrgCode}.sub";
            var request = new CreateOrgRequest
            {
                Code = subOrgCode,
                Name = "Sub Organization",
                OrgType = OrgTypeEnum.Default
            };

            // Act
            var subOrg = await uow.Request(request);
            await uow.SaveChanges();

            // Assert
            subOrg.Code.ShouldBe(subOrgCode);
            subOrg.ParentOrgCode.ShouldBe(parentOrgCode);
        }

        [Fact]
        public async Task GetParentOrgShouldWork()
        {
            // Arrange
            var service = _factory.Services;
            var uow = service.GetService<IUnitOfWork>();
            var parentOrgCode = $"parent_{ObjectId.GenerateNewId()}";
            var parentOrg = await CreateTestOrg(uow, parentOrgCode);
            var subOrg = await CreateTestOrg(uow, $"{parentOrgCode}.sub");
            await uow.SaveChanges();

            // Act
            using var verifyService = service.CreateScope();
            var verifyUow = verifyService.ServiceProvider.GetService<IUnitOfWork>();
            var retrievedSubOrg = verifyUow.Query<IOrg>().First(x => x.Code == $"{parentOrgCode}.sub");
            var retrievedParentOrg = retrievedSubOrg.ParentOrg;

            // Assert
            retrievedParentOrg.ShouldNotBeNull();
            retrievedParentOrg.Code.ShouldBe(parentOrgCode);
        }

        [Fact]
        public async Task GetAllParentOrgCodesShouldWork()
        {
            // Arrange
            var service = _factory.Services;
            var uow = service.GetService<IUnitOfWork>();
            var level1Code = $"level1_{ObjectId.GenerateNewId()}";
            await CreateTestOrg(uow, level1Code);
            await CreateTestOrg(uow, $"{level1Code}.level2");
            var deepOrg = await CreateTestOrg(uow, $"{level1Code}.level2.level3");
            await uow.SaveChanges();

            // Act
            var allParentCodes = deepOrg.AllParentOrgCodes;

            // Assert
            allParentCodes.ShouldContain(level1Code);
            allParentCodes.ShouldContain($"{level1Code}.level2");
            allParentCodes.Count.ShouldBe(2);
        }

        [Fact]
        public async Task GetDirectSubOrgsShouldWork()
        {
            // Arrange
            var service = _factory.Services;
            var uow = service.GetService<IUnitOfWork>();
            var parentCode = $"parent_{ObjectId.GenerateNewId()}";
            var parentOrg = await CreateTestOrg(uow, parentCode);
            await CreateTestOrg(uow, $"{parentCode}.sub1");
            await CreateTestOrg(uow, $"{parentCode}.sub2");
            await CreateTestOrg(uow, $"{parentCode}.sub1.subsub"); // This should not be included in direct subs
            await uow.SaveChanges();

            // Act
            var directSubCodes = parentOrg.DirectSubOrgCodes;

            // Assert
            directSubCodes.ShouldContain($"{parentCode}.sub1");
            directSubCodes.ShouldContain($"{parentCode}.sub2");
            directSubCodes.ShouldNotContain($"{parentCode}.sub1.subsub");
            directSubCodes.Count.ShouldBe(2);
        }

        [Fact]
        public async Task GetAllSubOrgsShouldWork()
        {
            // Arrange
            var service = _factory.Services;
            var uow = service.GetService<IUnitOfWork>();
            var parentCode = $"parent_{ObjectId.GenerateNewId()}";
            var parentOrg = await CreateTestOrg(uow, parentCode);
            await CreateTestOrg(uow, $"{parentCode}.sub1");
            await CreateTestOrg(uow, $"{parentCode}.sub2");
            await CreateTestOrg(uow, $"{parentCode}.sub1.subsub");
            await uow.SaveChanges();

            // Act
            var allSubCodes = parentOrg.AllSubOrgCodes;

            // Assert
            allSubCodes.ShouldContain($"{parentCode}.sub1");
            allSubCodes.ShouldContain($"{parentCode}.sub2");
            allSubCodes.ShouldContain($"{parentCode}.sub1.subsub");
            allSubCodes.Count.ShouldBe(3);
        }

        [Fact]
        public async Task ChangeOrgCodeShouldWork()
        {
            // Arrange
            var service = _factory.Services;
            var uow = service.GetService<IUnitOfWork>();
            var oldCode = $"oldcode_{ObjectId.GenerateNewId()}";
            var newCode = $"newcode_{ObjectId.GenerateNewId()}";
            
            // Create fresh orgs for this test
            var org = await CreateTestOrg(uow, oldCode);
            await CreateTestOrg(uow, $"{oldCode}.sub");
            await uow.SaveChanges();

            // Act
            org.SetCode(newCode);
            await uow.SaveChanges();

            // Assert
            org.Code.ShouldBe(newCode);
            using var verifyService = service.CreateScope();
            var verifyUow = verifyService.ServiceProvider.GetService<IUnitOfWork>();
            var subOrg = verifyUow.Query<IOrg>().FirstOrDefault(x => x.Code.StartsWith($"{newCode}."));
            subOrg.ShouldNotBeNull();
            subOrg.Code.ShouldBe($"{newCode}.sub");
        }

        [Fact]
        public async Task DeleteOrgShouldWork()
        {
            // Arrange
            var service = _factory.Services;
            var uow = service.GetService<IUnitOfWork>();
            var orgCode = $"deletetest_{ObjectId.GenerateNewId()}";
            var org = await CreateTestOrg(uow, orgCode, $"Delete Test Org {ObjectId.GenerateNewId()}");
            await uow.SaveChanges();
            var orgId = org.Id;

            // Act
            var deleteResult = await org.DeleteAsync();
            await uow.SaveChanges();

            // Assert
            deleteResult.ShouldBeGreaterThan(0);
            using var verifyService = service.CreateScope();
            var verifyUow = verifyService.ServiceProvider.GetService<IUnitOfWork>();
            var deletedOrg = verifyUow.Query<IOrg>().FirstOrDefault(x => x.Id == orgId);
            deletedOrg.ShouldBeNull();
        }

        [Fact]
        public async Task AssignUserToOrgWhenCreatingOrgShouldWork()
        {
            // Arrange
            var service = _factory.Services;
            var uow = service.GetService<IUnitOfWork>();
            var userUsername = $"orguser_{ObjectId.GenerateNewId()}";
            var user = await CreateTestUser(uow, userUsername);
            await uow.SaveChanges();
            
            var orgCode = $"assigntest_{ObjectId.GenerateNewId()}";
            var request = new CreateOrgRequest
            {
                Code = orgCode,
                Name = $"Assign Test Organization {ObjectId.GenerateNewId()}",
                OrgType = OrgTypeEnum.Default,
                CreateUserId = user.Id
            };

            // Act
            var org = await uow.Request(request);
            await uow.SaveChanges();

            // Assert
            using var verifyService = service.CreateScope();
            var verifyUow = verifyService.ServiceProvider.GetService<IUnitOfWork>();
            var updatedUser = verifyUow.Query<IUser>().First(x => x.Id == user.Id);
            updatedUser.OrgCodes.ShouldContain(org.Code);
        }

        [Fact]
        public async Task AutoAssignParentOrgUsersToSubOrgShouldWork()
        {
            // Arrange
            var service = _factory.Services;
            var uow = service.GetService<IUnitOfWork>();
            var userUsername = $"autouser_{ObjectId.GenerateNewId()}";
            var user = await CreateTestUser(uow, userUsername);
            var parentCode = $"autoparent_{ObjectId.GenerateNewId()}";
            var parentOrg = await CreateTestOrg(uow, parentCode);
            await uow.SaveChanges();
            
            // Assign user to parent org
            await user.AssignOrgs(new[] { parentOrg.Code });
            await uow.SaveChanges();

            var request = new CreateOrgRequest
            {
                Code = $"{parentCode}.sub",
                Name = $"Auto Sub Organization {ObjectId.GenerateNewId()}",
                OrgType = OrgTypeEnum.Default
            };

            // Act
            var subOrg = await uow.Request(request);
            await uow.SaveChanges();

            // Assert
            using var verifyService = service.CreateScope();
            var verifyUow = verifyService.ServiceProvider.GetService<IUnitOfWork>();
            var updatedUser = verifyUow.Query<IUser>().First(x => x.Id == user.Id);
            updatedUser.OrgCodes.ShouldContain(subOrg.Code);
        }

        [Fact]
        public async Task FixUserOrgShouldWork()
        {
            // Arrange
            var service = _factory.Services;
            var uow = service.GetService<IUnitOfWork>();
            var userUsername = $"fixuser_{ObjectId.GenerateNewId()}";
            var user = await CreateTestUser(uow, userUsername);
            await uow.SaveChanges();
            
            user.OrgCodes.Clear(); // Remove all orgs
            await uow.SaveChanges();

            var request = new FixUserOrgRequest();

            // Act
            var result = await uow.Request(request);
            await uow.SaveChanges();

            // Assert
            result.ShouldBe(true);
        }

        [Fact]
        public async Task QueryOrgsShouldWork()
        {
            // Arrange
            var service = _factory.Services;
            var uow = service.GetService<IUnitOfWork>();
            await CreateTestOrg(uow, $"org1_{ObjectId.GenerateNewId()}");
            await CreateTestOrg(uow, $"org2_{ObjectId.GenerateNewId()}");
            await uow.SaveChanges();

            // Act
            var orgs = uow.Query<IOrg>().ToList();

            // Assert
            orgs.Count.ShouldBeGreaterThanOrEqualTo(2);
        }

        [Fact]
        public async Task ValidateOrgShouldWork()
        {
            // Arrange
            var service = _factory.Services;
            var uow = service.GetService<IUnitOfWork>();
            var org = await CreateTestOrg(uow);
            await uow.SaveChanges();

            // Act
            var validationResult = await org.Validate(service);

            // Assert
            validationResult.ErrorMessage.ShouldBeNullOrEmpty();
        }

        private async Task<IOrg> CreateTestOrg(IUnitOfWork uow, string code = null, string name = null)
        {
            code ??= $"testorg_{ObjectId.GenerateNewId()}";
            name ??= $"Test Org {code}";
            
            var request = new CreateOrgRequest
            {
                Code = code,
                Name = name,
                OrgType = OrgTypeEnum.Default
            };

            return await uow.Request(request);
        }

        private async Task<IUser> CreateTestUser(IUnitOfWork uow, string username = null)
        {
            username ??= $"testuser_{ObjectId.GenerateNewId()}";
            var request = new CreateUserRequest
            {
                Username = username,
                Email = $"{username}@test.com",
                Password = "Password123!",
                Nickname = "Test User",
                IsEnable = true,
                RoleIds = new List<string>(),
                OrgCodes = new List<string>()
            };

            return await uow.Request(request);
        }
    }
}
