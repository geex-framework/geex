using Geex.Extensions.Authorization;
using Geex.Extensions.Identity.Core.Entities;
using Geex.Extensions.Identity.Requests;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Entities;

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
            var query = """
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
                """;

            var (responseData, responseString) = await client.PostGqlRequest( query);

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
            var query = """
                query($name: String!) {
                    roles(skip: 0, take: 10, filter: { name: { eq: $name }}) {
                        items {
                            id
                            code
                            name
                        }
                        totalCount
                    }
                }
                """;

            var (responseData, responseString) = await client.PostGqlRequest( query, new { name = targetRoleName });

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
            var query = """
                mutation($roleCode: String!, $roleName: String!) {
                    createRole(request: {
                        roleCode: $roleCode
                        roleName: $roleName
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
                """;

            var (responseData, responseString) = await client.PostGqlRequest( query, new { roleCode = testRoleCode, roleName = testRoleName });

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
            var query = """
                mutation($roleId: String!) {
                    setRoleDefault(request: { roleId: $roleId })
                }
                """;

            var (responseData, responseString) = await client.PostGqlRequest( query, new { roleId });

            bool setDefaultResult = (bool)responseData["data"]["setRoleDefault"];
            setDefaultResult.ShouldBeTrue();
        }

        [Fact]
        public async Task QueryRoleUsersShouldWork()
        {
            var roleIds = new string[3];
            var userIds = new string[4];
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                for (var index = 0; index < roleIds.Length; index++)
                {
                    var role = await uow.Request(new CreateRoleRequest
                    {
                        RoleCode = $"navigation_{ObjectId.GenerateNewId()}",
                        RoleName = $"Navigation role {index} {ObjectId.GenerateNewId()}"
                    });
                    roleIds[index] = role.Id;
                }

                string[][] memberships = [[roleIds[0]], [roleIds[1]], [roleIds[0], roleIds[1]], []];
                for (var index = 0; index < userIds.Length; index++)
                {
                    var user = await uow.Request(new CreateUserRequest
                    {
                        Username = $"navigation_{ObjectId.GenerateNewId()}",
                        Password = "Password123!".ToMd5(),
                        RoleIds = memberships[index].ToList()
                    });
                    userIds[index] = user.Id;
                }

                await uow.SaveChanges();
                await scope.ServiceProvider.GetRequiredService<IRbacEnforcer>()
                    .SetRoles(userIds[3], [roleIds[0]]);
            }

            using var client = SuperAdminClient;
            var query = """
                query($roleIds: [String!]!) {
                    roles(skip: 0, take: 10, filter: { id: { in: $roleIds } }) {
                        items {
                            id
                            code
                            users {
                                id
                                username
                            }
                        }
                        totalCount
                    }
                }
                """;

            await DB.RestartProfiler();
            try
            {
                var (responseData, responseString) = await client.PostGqlRequest(query, new { roleIds });

                responseData["errors"].ShouldBeNull(responseString);
                var roles = responseData["data"]!["roles"]!;
                roles["totalCount"]!.GetValue<int>().ShouldBe(3);
                var items = roles["items"]!.AsArray();
                items.Count.ShouldBe(3);
                string[][] expectedMembers = [[userIds[0], userIds[2], userIds[3]], [userIds[1], userIds[2]], []];
                for (var index = 0; index < roleIds.Length; index++)
                {
                    var role = items.Single(item => item!["id"]!.GetValue<string>() == roleIds[index])!;
                    role["users"]!.AsArray().Select(user => user!["id"]!.GetValue<string>()).OrderBy(id => id)
                        .ShouldBe(expectedMembers[index].OrderBy(id => id));
                }

                if (ScopedService.GetRequiredService<GeexCoreModuleOptions>().AutoBatchLoad)
                {
                    var userQueries = DB.GetProfilerLogs().AsQueryable()
                        .Where(entry => entry.ns == DB.DefaultDb.DatabaseNamespace.DatabaseName + "." + nameof(User) &&
                            (entry.op == "query" || entry.op == "command"))
                        .ToList();
                    userQueries.Count.ShouldBe(2, "One lookup authenticates the user and one batch loads the role members.");
                    userQueries.Count(entry => entry.nreturned == userIds.Length).ShouldBe(1);
                }
            }
            finally
            {
                DB.StopProfiler();
            }
        }

        [Fact]
        public async Task QueryRoleUsersWithNoMatchingRolesShouldReturnEmpty()
        {
            using var client = SuperAdminClient;
            var (responseData, responseString) = await client.PostGqlRequest(
                """
                query($roleId: String!) {
                    roles(skip: 0, take: 10, filter: { id: { eq: $roleId } }) {
                        items { id users { id } }
                        totalCount
                    }
                }
                """, new { roleId = ObjectId.GenerateNewId().ToString() });

            responseData["errors"].ShouldBeNull(responseString);
            var roles = responseData["data"]!["roles"]!;
            roles["items"]!.AsArray().ShouldBeEmpty();
            roles["totalCount"]!.GetValue<int>().ShouldBe(0);
        }

        [Fact]
        public async Task QueryRolePermissionsShouldWork()
        {
            // Arrange & Act & Assert - GraphQL queries don't need separate scopes
            var client = this.SuperAdminClient;
            var query = """
                query {
                    roles(skip: 0, take: 10) {
                        items {
                            id
                            code
                            permissions
                        }
                    }
                }
                """;

            var (responseData, responseString) = await client.PostGqlRequest( query);

            var items = responseData["data"]["roles"]["items"].AsArray();
            foreach (var role in items)
            {
                role["permissions"].ShouldNotBeNull();
            }
        }

        [Fact]
        public async Task QueryRolesCache_AsAnonymous_ShouldReturnEmptyWithoutAuthError()
        {
            var (responseData, responseString) = await AnonymousClient.PostGqlRequest(
                """
                query rolesCache {
                    rolesCache {
                        id
                    }
                }
                """);

            responseData["errors"].ShouldBeNull(responseString);
            responseData["data"].ShouldNotBeNull(responseString);
            responseData["data"]["rolesCache"].AsArray().Count.ShouldBe(0);
        }

        [Fact]
        public async Task QueryRolesCache_AsSuperAdmin_ShouldReturnRoles()
        {
            var (responseData, responseString) = await SuperAdminClient.PostGqlRequest(
                """
                query rolesCache {
                    rolesCache {
                        id
                    }
                }
                """);

            responseData["errors"].ShouldBeNull(responseString);
            responseData["data"]["rolesCache"].AsArray().Count.ShouldBeGreaterThan(0);
        }

        [Fact]
        public async Task QueryRoles_AsAnonymous_ShouldFail()
        {
            var (responseData, responseString) = await AnonymousClient.PostGqlRequest(
                """
                query {
                    roles(skip: 0, take: 10) {
                        items {
                            id
                        }
                    }
                }
                """,
                true);

            responseData["errors"].ShouldNotBeNull(responseString);
            var errorCode = (string)responseData["errors"][0]["extensions"]["code"];
            (errorCode == "AUTH_NOT_AUTHENTICATED" || errorCode == "AUTH_NOT_AUTHORIZED").ShouldBeTrue();
        }
    }
}
