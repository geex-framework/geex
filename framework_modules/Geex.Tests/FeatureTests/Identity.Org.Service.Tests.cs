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
    public class IdentityOrgServiceTests : TestsBase
    {
        public IdentityOrgServiceTests(TestApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task CreateOrgServiceShouldWork()
        {
            // Arrange
            var testOrgCode = $"testorg_{ObjectId.GenerateNewId()}";

            // Act
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetService<IUnitOfWork>();
                
                var request = new CreateOrgRequest
                {
                    Code = testOrgCode,
                    Name = "Test Organization",
                    OrgType = OrgTypeEnum.Default
                };

                var org = await uow.Request(request);
                await uow.SaveChanges();

                // Assert
                org.ShouldNotBeNull();
                org.Code.ShouldBe(testOrgCode);
                org.Name.ShouldBe("Test Organization");
                org.OrgType.ShouldBe(OrgTypeEnum.Default);
            }
        }

        [Fact]
        public async Task CreateSubOrganizationShouldWork()
        {
            // Arrange
            var parentOrgCode = $"parent_{ObjectId.GenerateNewId()}";
            var subOrgCode = $"{parentOrgCode}.sub";

            // Create parent org
            using (var parentScope = ScopedService.CreateScope())
            {
                var parentUow = parentScope.ServiceProvider.GetService<IUnitOfWork>();
                await CreateTestOrg(parentUow, parentOrgCode);
                await parentUow.SaveChanges();
            }

            // Act - Create sub org
            using (var subScope = ScopedService.CreateScope())
            {
                var subUow = subScope.ServiceProvider.GetService<IUnitOfWork>();
                
                var request = new CreateOrgRequest
                {
                    Code = subOrgCode,
                    Name = "Sub Organization",
                    OrgType = OrgTypeEnum.Default
                };

                var subOrg = await subUow.Request(request);
                await subUow.SaveChanges();

                // Assert
                subOrg.Code.ShouldBe(subOrgCode);
                subOrg.ParentOrgCode.ShouldBe(parentOrgCode);
            }
        }

        [Fact]
        public async Task GetParentOrgShouldWork()
        {
            // Arrange
            var parentOrgCode = $"parent_{ObjectId.GenerateNewId()}";
            var subOrgCode = $"{parentOrgCode}.sub";

            // Create orgs
            using (var setupScope = ScopedService.CreateScope())
            {
                var setupUow = setupScope.ServiceProvider.GetService<IUnitOfWork>();
                await CreateTestOrg(setupUow, parentOrgCode);
                await CreateTestOrg(setupUow, subOrgCode);
                await setupUow.SaveChanges();
            }

            // Act & Assert
            using (var verifyScope = ScopedService.CreateScope())
            {
                var verifyUow = verifyScope.ServiceProvider.GetService<IUnitOfWork>();
                var retrievedSubOrg = verifyUow.Query<IOrg>().First(x => x.Code == subOrgCode);
                var retrievedParentOrg = retrievedSubOrg.ParentOrg;

                retrievedParentOrg.ShouldNotBeNull();
                retrievedParentOrg.Code.ShouldBe(parentOrgCode);
            }
        }

        [Fact]
        public async Task GetAllParentOrgCodesShouldWork()
        {
            // Arrange
            var level1Code = $"level1_{ObjectId.GenerateNewId()}";
            var level2Code = $"{level1Code}.level2";
            var level3Code = $"{level2Code}.level3";

            // Create orgs
            using (var setupScope = ScopedService.CreateScope())
            {
                var setupUow = setupScope.ServiceProvider.GetService<IUnitOfWork>();
                await CreateTestOrg(setupUow, level1Code);
                await CreateTestOrg(setupUow, level2Code);
                var deepOrg = await CreateTestOrg(setupUow, level3Code);
                await setupUow.SaveChanges();

                // Act
                var allParentCodes = deepOrg.AllParentOrgCodes;

                // Assert
                allParentCodes.ShouldContain(level1Code);
                allParentCodes.ShouldContain(level2Code);
                allParentCodes.Count.ShouldBe(2);
            }
        }

        [Fact]
        public async Task GetDirectSubOrgsShouldWork()
        {
            // Arrange
            var parentCode = $"parent_{ObjectId.GenerateNewId()}";
            var sub1Code = $"{parentCode}.sub1";
            var sub2Code = $"{parentCode}.sub2";
            var subsubCode = $"{sub1Code}.subsub";

            // Act
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetService<IUnitOfWork>();
                var parentOrg = await CreateTestOrg(uow, parentCode);
                await CreateTestOrg(uow, sub1Code);
                await CreateTestOrg(uow, sub2Code);
                await CreateTestOrg(uow, subsubCode);
                await uow.SaveChanges();

                var directSubCodes = parentOrg.DirectSubOrgCodes;

                // Assert
                directSubCodes.ShouldContain(sub1Code);
                directSubCodes.ShouldContain(sub2Code);
                directSubCodes.ShouldNotContain(subsubCode);
                directSubCodes.Count.ShouldBe(2);
            }
        }

        [Fact]
        public async Task GetAllSubOrgsShouldWork()
        {
            // Arrange
            var parentCode = $"parent_{ObjectId.GenerateNewId()}";
            var sub1Code = $"{parentCode}.sub1";
            var sub2Code = $"{parentCode}.sub2";
            var subsubCode = $"{sub1Code}.subsub";

            // Act
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetService<IUnitOfWork>();
                var parentOrg = await CreateTestOrg(uow, parentCode);
                await CreateTestOrg(uow, sub1Code);
                await CreateTestOrg(uow, sub2Code);
                await CreateTestOrg(uow, subsubCode);
                await uow.SaveChanges();

                var allSubCodes = parentOrg.AllSubOrgCodes;

                // Assert
                allSubCodes.ShouldContain(sub1Code);
                allSubCodes.ShouldContain(sub2Code);
                allSubCodes.ShouldContain(subsubCode);
                allSubCodes.Count.ShouldBe(3);
            }
        }

        [Fact]
        public async Task ChangeOrgCodeShouldWork()
        {
            // Arrange
            var oldCode = $"oldcode_{ObjectId.GenerateNewId()}";
            var newCode = $"newcode_{ObjectId.GenerateNewId()}";
            var subOrgCode = $"{oldCode}.sub";

            // Create orgs
            using (var setupScope = ScopedService.CreateScope())
            {
                var setupUow = setupScope.ServiceProvider.GetService<IUnitOfWork>();
                var org = await CreateTestOrg(setupUow, oldCode);
                await CreateTestOrg(setupUow, subOrgCode);
                await setupUow.SaveChanges();

                // Act
                org.SetCode(newCode);
                await setupUow.SaveChanges();

                // Assert
                org.Code.ShouldBe(newCode);
            }

            // Verify sub org code was updated
            using (var verifyScope = ScopedService.CreateScope())
            {
                var verifyUow = verifyScope.ServiceProvider.GetService<IUnitOfWork>();
                var subOrg = verifyUow.Query<IOrg>().FirstOrDefault(x => x.Code.StartsWith($"{newCode}."));
                subOrg.ShouldNotBeNull();
                subOrg.Code.ShouldBe($"{newCode}.sub");
            }
        }

        [Fact]
        public async Task DeleteOrgShouldWork()
        {
            // Arrange
            var orgCode = $"deletetest_{ObjectId.GenerateNewId()}";
            string orgId;

            // Create org
            using (var setupScope = ScopedService.CreateScope())
            {
                var setupUow = setupScope.ServiceProvider.GetService<IUnitOfWork>();
                var org = await CreateTestOrg(setupUow, orgCode, $"Delete Test Org {ObjectId.GenerateNewId()}");
                await setupUow.SaveChanges();
                orgId = org.Id;
            }

            // Act
            long deleteResult;
            using (var deleteScope = ScopedService.CreateScope())
            {
                var deleteUow = deleteScope.ServiceProvider.GetService<IUnitOfWork>();
                var org = deleteUow.Query<IOrg>().First(x => x.Id == orgId);
                deleteResult = await org.DeleteAsync();
                await deleteUow.SaveChanges();
            }

            // Assert
            deleteResult.ShouldBeGreaterThan(0);
            using (var verifyScope = ScopedService.CreateScope())
            {
                var verifyUow = verifyScope.ServiceProvider.GetService<IUnitOfWork>();
                var deletedOrg = verifyUow.Query<IOrg>().FirstOrDefault(x => x.Id == orgId);
                deletedOrg.ShouldBeNull();
            }
        }

        [Fact]
        public async Task AssignUserToOrgWhenCreatingOrgShouldWork()
        {
            // Arrange
            var userUsername = $"orguser_{ObjectId.GenerateNewId()}";
            var orgCode = $"assigntest_{ObjectId.GenerateNewId()}";
            string userId;

            // Create user
            using (var setupScope = ScopedService.CreateScope())
            {
                var setupUow = setupScope.ServiceProvider.GetService<IUnitOfWork>();
                var user = await CreateTestUser(setupUow, userUsername);
                await setupUow.SaveChanges();
                userId = user.Id;
            }

            // Act - Create org with user assignment
            using (var orgScope = ScopedService.CreateScope())
            {
                var orgUow = orgScope.ServiceProvider.GetService<IUnitOfWork>();
                
                var request = new CreateOrgRequest
                {
                    Code = orgCode,
                    Name = $"Assign Test Organization {ObjectId.GenerateNewId()}",
                    OrgType = OrgTypeEnum.Default,
                    CreateUserId = userId
                };

                var org = await orgUow.Request(request);
                await orgUow.SaveChanges();
            }

            // Assert
            using (var verifyScope = ScopedService.CreateScope())
            {
                var verifyUow = verifyScope.ServiceProvider.GetService<IUnitOfWork>();
                var updatedUser = verifyUow.Query<IUser>().First(x => x.Id == userId);
                updatedUser.OrgCodes.ShouldContain(orgCode);
            }
        }

        [Fact]
        public async Task AutoAssignParentOrgUsersToSubOrgShouldWork()
        {
            // Arrange
            var userUsername = $"autouser_{ObjectId.GenerateNewId()}";
            var parentCode = $"autoparent_{ObjectId.GenerateNewId()}";
            var subOrgCode = $"{parentCode}.sub";
            string userId;

            // Create user and parent org
            using (var setupScope = ScopedService.CreateScope())
            {
                var setupUow = setupScope.ServiceProvider.GetService<IUnitOfWork>();
                var user = await CreateTestUser(setupUow, userUsername);
                var parentOrg = await CreateTestOrg(setupUow, parentCode);
                await setupUow.SaveChanges();
                userId = user.Id;

                // Assign user to parent org
                await user.AssignOrgs(new[] { parentOrg.Code });
                await setupUow.SaveChanges();
            }

            // Act - Create sub org
            using (var subOrgScope = ScopedService.CreateScope())
            {
                var subOrgUow = subOrgScope.ServiceProvider.GetService<IUnitOfWork>();
                
                var request = new CreateOrgRequest
                {
                    Code = subOrgCode,
                    Name = $"Auto Sub Organization {ObjectId.GenerateNewId()}",
                    OrgType = OrgTypeEnum.Default
                };

                var subOrg = await subOrgUow.Request(request);
                await subOrgUow.SaveChanges();
            }

            // Assert
            using (var verifyScope = ScopedService.CreateScope())
            {
                var verifyUow = verifyScope.ServiceProvider.GetService<IUnitOfWork>();
                var updatedUser = verifyUow.Query<IUser>().First(x => x.Id == userId);
                updatedUser.OrgCodes.ShouldContain(subOrgCode);
            }
        }

        [Fact]
        public async Task FixUserOrgShouldWork()
        {
            // Arrange
            var userUsername = $"fixuser_{ObjectId.GenerateNewId()}";
            string userId;

            // Create user and clear orgs
            using (var setupScope = ScopedService.CreateScope())
            {
                var setupUow = setupScope.ServiceProvider.GetService<IUnitOfWork>();
                var user = await CreateTestUser(setupUow, userUsername);
                await setupUow.SaveChanges();
                userId = user.Id;

                user.OrgCodes.Clear();
                await setupUow.SaveChanges();
            }

            // Act
            using (var fixScope = ScopedService.CreateScope())
            {
                var fixUow = fixScope.ServiceProvider.GetService<IUnitOfWork>();
                
                var request = new FixUserOrgRequest();
                var result = await fixUow.Request(request);
                await fixUow.SaveChanges();

                // Assert
                result.ShouldBe(true);
            }
        }

        [Fact]
        public async Task QueryOrgsShouldWork()
        {
            // Arrange & Act
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetService<IUnitOfWork>();
                await CreateTestOrg(uow, $"org1_{ObjectId.GenerateNewId()}");
                await CreateTestOrg(uow, $"org2_{ObjectId.GenerateNewId()}");
                await uow.SaveChanges();

                var orgs = uow.Query<IOrg>().ToList();

                // Assert
                orgs.Count.ShouldBeGreaterThanOrEqualTo(2);
            }
        }

        [Fact]
        public async Task ValidateOrgShouldWork()
        {
            // Arrange & Act
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetService<IUnitOfWork>();
                var org = await CreateTestOrg(uow);
                await uow.SaveChanges();

                var validationResult = await org.Validate(scope.ServiceProvider);

                // Assert
                validationResult.ErrorMessage.ShouldBeNullOrEmpty();
            }
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
