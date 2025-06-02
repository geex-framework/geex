using Geex.Extensions.Identity;
using Geex.Extensions.Identity.Requests;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using Newtonsoft.Json;

namespace Geex.Tests.FeatureTests
{
    [Collection(nameof(TestsCollection))]
    public class IdentityRoleApiTests : TestsBase
    {
        public IdentityRoleApiTests(TestApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task QueryRolesShouldWork()
        {
            // Arrange & Act & Assert - GraphQL queries don't need separate scopes
            var client = this.SuperAdminClient;
            var request = new
            {
                query = $$"""
                    query {
                        roles(skip: 0, take: 10) {
                            items {
                                id
                                code
                                name
                                isStatic
                                isDefault
                                isEnabled
                                permissions
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

            var response = await client.PostAsync(GqlEndpoint, content);
            var (responseData, responseString) = await response.ParseGraphQLResponse();

            responseData["data"]["roles"]["totalCount"].GetValue<int>().ShouldBeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public async Task FilterRolesByNameShouldWork()
        {
            // Arrange - Prepare data using separate scope
            var targetRoleName = $"Test Role {ObjectId.GenerateNewId()}";
            var targetRoleCode = $"testrole_{ObjectId.GenerateNewId()}";

            using (var scope = ScopedService.CreateScope())
            {
                var setupUow = scope.ServiceProvider.GetService<IUnitOfWork>();
                await setupUow.Request(new CreateRoleRequest
                {
                    RoleCode = targetRoleCode,
                    RoleName = targetRoleName,
                    IsStatic = false,
                    IsDefault = false
                });
                await setupUow.SaveChanges();
            }

            // Act & Assert - Query using GraphQL
            var client = this.SuperAdminClient;
            var request = new
            {
                query = $$$"""
                    query {
                        roles(skip: 0, take: 10, filter: { name: { eq: "{{{targetRoleName}}}" }}) {
                            items {
                                id
                                code
                                name
                            }
                            totalCount
                        }
                    }
                    """
            };

            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            var response = await client.PostAsync(GqlEndpoint, content);
            var (responseData, responseString) = await response.ParseGraphQLResponse();

            var items = responseData["data"]["roles"]["items"].AsArray();
            items.Count.ShouldBeGreaterThan(0);
            ((string)items[0]["name"]).ShouldBe(targetRoleName);
        }

        [Fact]
        public async Task CreateRoleMutationShouldWork()
        {
            // Arrange
            var testRoleCode = $"newrole_{ObjectId.GenerateNewId()}";
            var testRoleName = $"New Role {ObjectId.GenerateNewId()}";

            // Act & Assert - GraphQL mutation
            var client = this.SuperAdminClient;
            var request = new
            {
                query = $$$"""
                    mutation {
                        createRole(request: {
                            roleCode: "{{{testRoleCode}}}"
                            roleName: "{{{testRoleName}}}"
                            isStatic: false
                            isDefault: false
                        }) {
                            id
                            code
                            name
                            isStatic
                            isDefault
                            isEnabled
                        }
                    }
                    """
            };

            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            var response = await client.PostAsync(GqlEndpoint, content);
            var (responseData, responseString) = await response.ParseGraphQLResponse();

            var createdRole = responseData["data"]["createRole"];
            ((string)createdRole["code"]).ShouldBe(testRoleCode);
            ((string)createdRole["name"]).ShouldBe(testRoleName);
            ((bool)createdRole["isStatic"]).ShouldBe(false);
            ((bool)createdRole["isDefault"]).ShouldBe(false);
        }

        [Fact]
        public async Task SetRoleDefaultMutationShouldWork()
        {
            // Arrange - Prepare data using separate scope
            var testRoleCode = $"defaultapi_{ObjectId.GenerateNewId()}";
            string roleId;

            using (var scope = ScopedService.CreateScope())
            {
                var setupUow = scope.ServiceProvider.GetService<IUnitOfWork>();
                var role = await setupUow.Request(new CreateRoleRequest
                {
                    RoleCode = testRoleCode,
                    RoleName = $"Default API Role {ObjectId.GenerateNewId()}",
                    IsStatic = false,
                    IsDefault = false
                });
                await setupUow.SaveChanges();
                roleId = role.Id;
            }

            // Act & Assert - GraphQL mutation
            var client = this.SuperAdminClient;
            var request = new
            {
                query = $$$"""
                    mutation {
                        setRoleDefault(request: { roleId: "{{{roleId}}}" })
                    }
                    """
            };

            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            var response = await client.PostAsync(GqlEndpoint, content);
            var (responseData, responseString) = await response.ParseGraphQLResponse();

            bool setDefaultResult = (bool)responseData["data"]["setRoleDefault"];
            setDefaultResult.ShouldBeTrue();
        }

        [Fact]
        public async Task QueryRoleUsersShouldWork()
        {
            // Arrange & Act & Assert - GraphQL queries don't need separate scopes
            var client = this.SuperAdminClient;
            var request = new
            {
                query = $$"""
                    query {
                        roles(skip: 0, take: 10) {
                            items {
                                id
                                code
                                users {
                                    id
                                    username
                                }
                            }
                        }
                    }
                    """
            };

            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            var response = await client.PostAsync(GqlEndpoint, content);
            var (responseData, responseString) = await response.ParseGraphQLResponse();

            var items = responseData["data"]["roles"]["items"].AsArray();
            foreach (var role in items)
            {
                role["users"].ShouldNotBeNull();
            }
        }

        [Fact]
        public async Task QueryRolePermissionsShouldWork()
        {
            // Arrange & Act & Assert - GraphQL queries don't need separate scopes
            var client = this.SuperAdminClient;
            var request = new
            {
                query = $$"""
                    query {
                        roles(skip: 0, take: 10) {
                            items {
                                id
                                code
                                permissions
                            }
                        }
                    }
                    """
            };

            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            var response = await client.PostAsync(GqlEndpoint, content);
            var (responseData, responseString) = await response.ParseGraphQLResponse();

            var items = responseData["data"]["roles"]["items"].AsArray();
            foreach (var role in items)
            {
                role["permissions"].ShouldNotBeNull();
            }
        }
    }
}
