using Geex.Extensions.Identity;
using Geex.Extensions.Identity.Requests;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Geex.Abstractions;
using Geex.Extensions.Authentication.Requests;
using MongoDB.Bson;
using Newtonsoft.Json;

namespace Geex.Tests.FeatureTests
{
    [Collection(nameof(TestsCollection))]
    public class IdentityUserApiTests : TestsBase
    {
        public IdentityUserApiTests(TestApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task QueryUsersShouldWork()
        {
            // Arrange
            var client = this.SuperAdminClient;
            var query = """
                query {
                    users(skip: 0, take: 10) {
                        items {
                            id
                            username
                            email
                            nickname
                            isEnable
                            phoneNumber
                            orgCodes
                            roleIds
                        }
                        pageInfo {
                            hasPreviousPage
                            hasNextPage
                        }
                        totalCount
                    }
                }
                """;

            // Act
            var (responseData, responseString) = await client.PostGqlRequest( query);

            // Assert
            responseData["data"]["users"]["totalCount"].GetValue<int>().ShouldBeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public async Task FilterUsersByUsernameShouldWork()
        {
            // Arrange
            var client = this.SuperAdminClient;
            var targetUsername = $"testuser_{ObjectId.GenerateNewId()}";

            // Prepare data using separate scope
            using (var scope = ScopedService.CreateScope())
            {
                var setupUow = scope.ServiceProvider.GetService<IUnitOfWork>();
                await setupUow.Request(new CreateUserRequest
                {
                    Username = targetUsername,
                    Email = $"{targetUsername}@test.com",
                    Password = "Password123!",
                    Nickname = "Test User",
                    IsEnable = true,
                    RoleIds = new List<string>(),
                    OrgCodes = new List<string>()
                });
                await setupUow.SaveChanges();
            }

            var query = """
                query($username: String!) {
                    users(skip: 0, take: 10, filter: { username: { eq: $username }}) {
                        items {
                            id
                            username
                            email
                            nickname
                        }
                        totalCount
                    }
                }
                """;

            // Act
            var (responseData, responseString) = await client.PostGqlRequest( query, new { username = targetUsername });

            // Assert
            var items = responseData["data"]["users"]["items"].AsArray();
            items.Count.ShouldBeGreaterThan(0);
            ((string)items[0]["username"]).ShouldBe(targetUsername);
        }

        [Fact]
        public async Task QueryCurrentUserShouldWork()
        {
            // Arrange
            var client = this.SuperAdminClient;
            var query = """
                query {
                    currentUser {
                        id
                        username
                        email
                        nickname
                    }
                }
                """;

            // Act
            var (responseData, responseString) = await client.PostGqlRequest( query);

            // Assert
            responseData["data"]["currentUser"].ShouldNotBeNull();
        }

        [Fact]
        public async Task CreateUserMutationShouldWork()
        {
            // Arrange
            var client = this.SuperAdminClient;
            var testUsername = $"newuser_{ObjectId.GenerateNewId()}";

            var query = """
                mutation($username: String!, $email: String!) {
                    createUser(request: {
                        username: $username
                        email: $email
                        password: "Password123!"
                        nickname: "New User"
                        isEnable: true
                        roleIds: []
                        orgCodes: []
                    }) {
                        id
                        username
                        email
                        nickname
                        isEnable
                    }
                }
                """;

            // Act
            var (responseData, responseString) = await client.PostGqlRequest( query, new { username = testUsername, email = $"{testUsername}@test.com" });

            // Assert
            var createdUser = responseData["data"]["createUser"];
            ((string)createdUser["username"]).ShouldBe(testUsername);
            ((string)createdUser["email"]).ShouldBe($"{testUsername}@test.com");
            ((bool)createdUser["isEnable"]).ShouldBe(true);
        }

        [Fact]
        public async Task EditUserMutationShouldWork()
        {
            // Arrange
            var client = this.SuperAdminClient;
            var testUsername = $"editapi_{ObjectId.GenerateNewId()}";
            var uniquePhoneNumber = $"156{ObjectId.GenerateNewId().ToString().Substring(0, 8)}";
            string userId;

            // Prepare data using separate scope
            using (var scope = ScopedService.CreateScope())
            {
                var setupUow = scope.ServiceProvider.GetService<IUnitOfWork>();
                var user = await setupUow.Request(new CreateUserRequest
                {
                    Username = testUsername,
                    Email = $"{testUsername}@test.com",
                    Password = "Password123!",
                    Nickname = "Original API Nickname",
                    IsEnable = true,
                    RoleIds = new List<string>(),
                    OrgCodes = new List<string>()
                });
                await setupUow.SaveChanges();
                userId = user.Id;
            }

            var query = """
                mutation($id: String!, $nickname: String!, $phoneNumber: String!) {
                    editUser(request: {
                        id: $id
                        nickname: $nickname
                        phoneNumber: $phoneNumber
                        isEnable: false
                    }) {
                        id
                        nickname
                        phoneNumber
                        isEnable
                    }
                }
                """;

            // Act
            var (responseData, responseString) = await client.PostGqlRequest( query, new { id = userId, nickname = "Updated API Nickname", phoneNumber = uniquePhoneNumber });

            // Assert
            var editedUser = responseData["data"]["editUser"];
            ((string)editedUser["nickname"]).ShouldBe("Updated API Nickname");
            ((string)editedUser["phoneNumber"]).ShouldBe(uniquePhoneNumber);
            ((bool)editedUser["isEnable"]).ShouldBe(false);
        }

        [Fact]
        public async Task DeleteUserMutationShouldWork()
        {
            // Arrange
            var client = this.SuperAdminClient;
            var testUsername = $"deleteapi_{ObjectId.GenerateNewId()}";
            string userId;

            // Prepare data using separate scope
            using (var scope = ScopedService.CreateScope())
            {
                var setupUow = scope.ServiceProvider.GetService<IUnitOfWork>();
                var user = await setupUow.Request(new CreateUserRequest
                {
                    Username = testUsername,
                    Email = $"{testUsername}@test.com",
                    Password = "Password123!",
                    Nickname = "Delete API User",
                    IsEnable = true,
                    RoleIds = new List<string>(),
                    OrgCodes = new List<string>()
                });
                await setupUow.SaveChanges();
                userId = user.Id;
            }

            var query = """
                mutation($id: String!) {
                    deleteUser(request: { id: $id })
                }
                """;

            // Act
            var (responseData, responseString) = await client.PostGqlRequest( query, new { id = userId });

            // Assert
            bool deleteResult = (bool)responseData["data"]["deleteUser"];
            deleteResult.ShouldBeTrue();

            // Verify the user is actually deleted in separate scope
            using (var verifyScope = ScopedService.CreateScope())
            {
                var verifyUow = verifyScope.ServiceProvider.GetService<IUnitOfWork>();
                var deletedUser = verifyUow.Query<IUser>().FirstOrDefault(x => x.Id == userId);
                deletedUser.ShouldBeNull();
            }
        }

        [Fact]
        public async Task ChangePasswordMutationShouldWork()
        {
            // Arrange
            var client = this.SuperAdminClient;
            var testUsername = $"changeapi_{ObjectId.GenerateNewId()}";
            var originalPassword = "OriginalPass123!";
            var newUserToken = string.Empty;
            
            // Prepare data using separate scope
            using (var scope = ScopedService.CreateScope())
            {
                var setupUow = scope.ServiceProvider.GetService<IUnitOfWork>();
                await setupUow.Query<IUser>().Where(x=>x.Username == testUsername).DeleteAsync();
                await setupUow.Request(new CreateUserRequest
                {
                    Username = testUsername,
                    Email = $"{testUsername}@test.com",
                    Password = originalPassword,
                    Nickname = "Change Password User",
                    IsEnable = true,
                    RoleIds = new List<string>(),
                    OrgCodes = new List<string>()
                });
                await setupUow.SaveChanges();
            }

            // Get authentication token in separate scope
            using (var authScope = ScopedService.CreateScope())
            {
                var authUow = authScope.ServiceProvider.GetService<IUnitOfWork>();
                var token = await authUow.Request(new AuthenticateRequest()
                {
                    Password = originalPassword,
                    UserIdentifier = testUsername
                });
                newUserToken = token.Value;
            }

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newUserToken);
            var query = """
                mutation($originPassword: String!) {
                    changePassword(request: {
                        originPassword: $originPassword
                        newPassword: "NewPassword123!"
                    })
                }
                """;

            // Act
            var (responseData, responseString) = await client.PostGqlRequest( query, new { originPassword = originalPassword });

            // Assert
            bool changeResult = (bool)responseData["data"]["changePassword"];
            changeResult.ShouldBeTrue();
        }

        [Fact]
        public async Task FilterUsersByIsEnableShouldWork()
        {
            // Arrange
            var client = this.SuperAdminClient;
            var query = """
                query {
                    users(skip: 0, take: 10, filter: { isEnable: { eq: true } }) {
                        items {
                            id
                            username
                            isEnable
                        }
                        totalCount
                    }
                }
                """;

            // Act
            var (responseData, responseString) = await client.PostGqlRequest( query);

            // Assert
            var items = responseData["data"]["users"]["items"].AsArray();
            foreach (var item in items)
            {
                ((bool)item["isEnable"]).ShouldBe(true);
            }
        }
    }
}
