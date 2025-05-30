using Geex.Extensions.Identity;
using Geex.Extensions.Identity.Requests;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using Newtonsoft.Json;

namespace Geex.Tests.FeatureTests
{
    [Collection(nameof(TestsCollection))]
    public class IdentityUserApiTests
    {
        private readonly TestApplicationFactory _factory;
        private readonly string _graphqlEndpoint = "/graphql";

        public IdentityUserApiTests(TestApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task QueryUsersShouldWork()
        {
            // Arrange
            var client = _factory.CreateClient();
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
            var response = await client.PostAsync(_graphqlEndpoint, content);
            var (responseData, responseString) = await response.ParseGraphQLResponse();

            // Assert
            responseData["data"]["users"]["totalCount"].GetValue<int>().ShouldBeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public async Task FilterUsersByUsernameShouldWork()
        {
            // Arrange
            var client = _factory.CreateClient();
            var targetUsername = $"testuser_{ObjectId.GenerateNewId()}";

            // First create a user with specific username
            var service = _factory.Services;
            var uow = service.GetService<IUnitOfWork>();

            var user = await uow.Request(new CreateUserRequest
            {
                Username = targetUsername,
                Email = $"{targetUsername}@test.com",
                Password = "Password123!",
                Nickname = "Test User",
                IsEnable = true,
                RoleIds = new List<string>(),
                OrgCodes = new List<string>()
            });
            await uow.SaveChanges();

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
            var response = await client.PostAsync(_graphqlEndpoint, content);
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
            var client = _factory.CreateClient();
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
            var response = await client.PostAsync(_graphqlEndpoint, content);
            var (responseData, responseString) = await response.ParseGraphQLResponse();

            // Assert
            responseData["data"]["currentUser"].ShouldNotBeNull();
        }

        [Fact]
        public async Task CreateUserMutationShouldWork()
        {
            // Arrange
            var client = _factory.CreateClient();
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
            var response = await client.PostAsync(_graphqlEndpoint, content);
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
            var client = _factory.CreateClient();
            var service = _factory.Services;
            var uow = service.GetService<IUnitOfWork>();

            // First create a user to edit
            var testUsername = $"edituser_{ObjectId.GenerateNewId()}";
            var user = await uow.Request(new CreateUserRequest
            {
                Username = testUsername,
                Email = $"{testUsername}@test.com",
                Password = "Password123!",
                Nickname = "Original Nickname",
                IsEnable = true,
                RoleIds = new List<string>(),
                OrgCodes = new List<string>()
            });
            await uow.SaveChanges();

            var request = new
            {
                query = $$$"""
                    mutation {
                        editUser(request: {
                            id: "{{{user.Id}}}"
                            nickname: "Updated Nickname"
                            phoneNumber: "9876543210"
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
            var response = await client.PostAsync(_graphqlEndpoint, content);
            var (responseData, responseString) = await response.ParseGraphQLResponse();

            // Assert
            var editedUser = responseData["data"]["editUser"];
            ((string)editedUser["nickname"]).ShouldBe("Updated Nickname");
            ((string)editedUser["phoneNumber"]).ShouldBe("9876543210");
            ((bool)editedUser["isEnable"]).ShouldBe(false);
        }

        [Fact]
        public async Task DeleteUserMutationShouldWork()
        {
            // Arrange
            var client = _factory.CreateClient();
            var service = _factory.Services;
            var uow = service.GetService<IUnitOfWork>();

            // First create a user to delete
            var testUsername = $"deleteuser_{ObjectId.GenerateNewId()}";
            var user = await uow.Request(new CreateUserRequest
            {
                Username = testUsername,
                Email = $"{testUsername}@test.com",
                Password = "Password123!",
                Nickname = "Test User",
                IsEnable = true,
                RoleIds = new List<string>(),
                OrgCodes = new List<string>()
            });
            await uow.SaveChanges();

            var request = new
            {
                query = $$$"""
                    mutation {
                        deleteUser(request: { id: "{{{user.Id}}}" })
                    }
                    """
            };

            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync(_graphqlEndpoint, content);
            var (responseData, responseString) = await response.ParseGraphQLResponse();

            // Assert
            bool deleteResult = (bool)responseData["data"]["deleteUser"];
            deleteResult.ShouldBeTrue();

            // Verify the user is actually deleted
            using var verifyService = service.CreateScope();
            var verifyUow = verifyService.ServiceProvider.GetService<IUnitOfWork>();
            var deletedUser = verifyUow.Query<IUser>().FirstOrDefault(x => x.Id == user.Id);
            deletedUser.ShouldBeNull();
        }

        [Fact]
        public async Task ChangePasswordMutationShouldWork()
        {
            // Arrange
            var client = _factory.CreateClient();

            var request = new
            {
                query = $$"""
                    mutation {
                        changePassword(request: {
                            originPassword: "admin"
                            newPassword: "NewPassword123!"
                        })
                    }
                    """
            };

            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync(_graphqlEndpoint, content);
            var (responseData, responseString) = await response.ParseGraphQLResponse();

            // Assert
            bool changeResult = (bool)responseData["data"]["changePassword"];
            changeResult.ShouldBeTrue();
        }

        [Fact]
        public async Task FilterUsersByIsEnableShouldWork()
        {
            // Arrange
            var client = _factory.CreateClient();
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
            var response = await client.PostAsync(_graphqlEndpoint, content);
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
