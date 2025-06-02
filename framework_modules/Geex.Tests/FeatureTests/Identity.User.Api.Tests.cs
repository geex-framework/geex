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
using Geex.Extensions.Requests.Authentication;
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
            var request = new
            {
                query = $$"""
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
                    """
            };

            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync(GqlEndpoint, content);
            var (responseData, responseString) = await response.ParseGraphQLResponse();

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

            var request = new
            {
                query = $$$"""
                    query {
                        users(skip: 0, take: 10, filter: { username: { eq: "{{{targetUsername}}}" }}) {
                            items {
                                id
                                username
                                email
                                nickname
                            }
                            totalCount
                        }
                    }
                    """
            };

            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync(GqlEndpoint, content);
            var (responseData, responseString) = await response.ParseGraphQLResponse();

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
            var request = new
            {
                query = $$"""
                    query {
                        currentUser {
                            id
                            username
                            email
                            nickname
                        }
                    }
                    """
            };

            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync(GqlEndpoint, content);
            var (responseData, responseString) = await response.ParseGraphQLResponse();

            // Assert
            responseData["data"]["currentUser"].ShouldNotBeNull();
        }

        [Fact]
        public async Task CreateUserMutationShouldWork()
        {
            // Arrange
            var client = this.SuperAdminClient;
            var testUsername = $"newuser_{ObjectId.GenerateNewId()}";

            var request = new
            {
                query = $$$"""
                    mutation {
                        createUser(request: {
                            username: "{{{testUsername}}}"
                            email: "{{{testUsername}}}@test.com"
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
                    """
            };

            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync(GqlEndpoint, content);
            var (responseData, responseString) = await response.ParseGraphQLResponse();

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

            var request = new
            {
                query = $$$"""
                    mutation {
                        editUser(request: {
                            id: "{{{userId}}}"
                            nickname: "Updated API Nickname"
                            phoneNumber: "{{{uniquePhoneNumber}}}"
                            isEnable: false
                        }) {
                            id
                            nickname
                            phoneNumber
                            isEnable
                        }
                    }
                    """
            };

            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync(GqlEndpoint, content);
            var (responseData, responseString) = await response.ParseGraphQLResponse();

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

            var request = new
            {
                query = $$$"""
                    mutation {
                        deleteUser(request: { id: "{{{userId}}}" })
                    }
                    """
            };

            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync(GqlEndpoint, content);
            var (responseData, responseString) = await response.ParseGraphQLResponse();

            // Assert
            bool deleteResult = (bool)responseData["data"]["deleteUser"];
            deleteResult.ShouldBeTrue();

            // Verify the user is actually deleted
            using var verifyService = ScopedService.CreateScope();
            var verifyUow = verifyService.ServiceProvider.GetService<IUnitOfWork>();
            var deletedUser = verifyUow.Query<IUser>().FirstOrDefault(x => x.Id == userId);
            deletedUser.ShouldBeNull();
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
                var token = await setupUow.Request(new AuthenticateRequest()
                {
                    Password = originalPassword,
                    UserIdentifier = testUsername
                });
                newUserToken = token.Value;
            }
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newUserToken);
            var request = new
            {
                query = $$"""
                    mutation {
                        changePassword(request: {
                            originPassword: "{{originalPassword}}"
                            newPassword: "NewPassword123!"
                        })
                    }
                    """
            };

            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync(GqlEndpoint, content);
            var (responseData, responseString) = await response.ParseGraphQLResponse();

            // Assert
            bool changeResult = (bool)responseData["data"]["changePassword"];
            changeResult.ShouldBeTrue();
        }

        [Fact]
        public async Task FilterUsersByIsEnableShouldWork()
        {
            // Arrange
            var client = this.SuperAdminClient;
            var request = new
            {
                query = $$"""
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
                    """
            };

            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync(GqlEndpoint, content);
            var (responseData, responseString) = await response.ParseGraphQLResponse();

            // Assert
            var items = responseData["data"]["users"]["items"].AsArray();
            foreach (var item in items)
            {
                ((bool)item["isEnable"]).ShouldBe(true);
            }
        }
    }
}
