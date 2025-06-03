using Geex.Extensions.Identity;
using Geex.Extensions.Identity.Core.Entities;
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
    public class IdentityOrgApiTests : TestsBase
    {
        public IdentityOrgApiTests(TestApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task QueryOrgsShouldWork()
        {
            // Arrange & Act & Assert - GraphQL queries don't need separate scopes
            var client = this.SuperAdminClient;
            var query = """
                query {
                    orgs(skip: 0, take: 10) {
                        items {
                            id
                            code
                            name
                            orgType
                            parentOrgCode
                            allParentOrgCodes
                            allSubOrgCodes
                            directSubOrgCodes
                        }
                        pageInfo {
                            hasPreviousPage
                            hasNextPage
                        }
                        totalCount
                    }
                }
                """;

            var (responseData, responseString) = await client.PostGqlRequest(query);

            responseData["data"]["orgs"]["totalCount"].GetValue<int>().ShouldBeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public async Task FilterOrgsByCodeShouldWork()
        {
            // Arrange - Prepare data using separate scope
            var targetOrgCode = $"testorg_{ObjectId.GenerateNewId()}";

            using (var scope = ScopedService.CreateScope())
            {
                var setupUow = scope.ServiceProvider.GetService<IUnitOfWork>();
                await setupUow.Request(new CreateOrgRequest
                {
                    Code = targetOrgCode,
                    Name = $"Test Org {ObjectId.GenerateNewId()}",
                    OrgType = OrgTypeEnum.Default
                });
                await setupUow.SaveChanges();
            }

            // Act & Assert - Query using GraphQL
            var client = this.SuperAdminClient;
            var query = """
                query($code: String!) {
                    orgs(skip: 0, take: 10, filter: { code: { eq: $code }}) {
                        items {
                            id
                            code
                            name
                        }
                        totalCount
                    }
                }
                """;

            var (responseData, responseString) = await client.PostGqlRequest(query, new { code = targetOrgCode });

            var items = responseData["data"]["orgs"]["items"].AsArray();
            items.Count.ShouldBeGreaterThan(0);
            ((string)items[0]["code"]).ShouldBe(targetOrgCode);
        }

        [Fact]
        public async Task FilterOrgsByNameShouldWork()
        {
            // Arrange - Prepare data using separate scope
            var targetOrgName = $"Unique Org Name {ObjectId.GenerateNewId()}";
            var orgCode = $"uniqueorg_{ObjectId.GenerateNewId()}";

            using (var scope = ScopedService.CreateScope())
            {
                var setupUow = scope.ServiceProvider.GetService<IUnitOfWork>();
                await setupUow.Request(new CreateOrgRequest
                {
                    Code = orgCode,
                    Name = targetOrgName,
                    OrgType = OrgTypeEnum.Default
                });
                await setupUow.SaveChanges();
            }

            // Act & Assert - Query using GraphQL
            var client = this.SuperAdminClient;
            var query = """
                query($name: String!) {
                    orgs(skip: 0, take: 10, filter: { name: { eq: $name }}) {
                        items {
                            id
                            code
                            name
                        }
                        totalCount
                    }
                }
                """;

            var (responseData, responseString) = await client.PostGqlRequest(query, new { name = targetOrgName });

            var items = responseData["data"]["orgs"]["items"].AsArray();
            items.Count.ShouldBeGreaterThan(0);
            ((string)items[0]["name"]).ShouldBe(targetOrgName);
        }

        [Fact]
        public async Task CreateOrgMutationShouldWork()
        {
            // Arrange
            var testOrgCode = $"neworg_{ObjectId.GenerateNewId()}";
            var testOrgName = $"New Organization {ObjectId.GenerateNewId()}";

            // Act & Assert - GraphQL mutation
            var client = this.SuperAdminClient;
            var query = """
                mutation($code: String!, $name: String!) {
                    createOrg(request: {
                        code: $code
                        name: $name
                        orgType: Default
                    }) {
                        id
                        code
                        name
                        orgType
                    }
                }
                """;

            var (responseData, responseString) = await client.PostGqlRequest(query, new { code = testOrgCode, name = testOrgName });

            var createdOrg = responseData["data"]["createOrg"];
            ((string)createdOrg["code"]).ShouldBe(testOrgCode);
            ((string)createdOrg["name"]).ShouldBe(testOrgName);
        }

        [Fact]
        public async Task CreateSubOrgMutationShouldWork()
        {
            // Arrange - Prepare parent org data using separate scope
            var parentOrgCode = $"parentorg_{ObjectId.GenerateNewId()}";

            using (var scope = ScopedService.CreateScope())
            {
                var setupUow = scope.ServiceProvider.GetService<IUnitOfWork>();
                await setupUow.Request(new CreateOrgRequest
                {
                    Code = parentOrgCode,
                    Name = $"Parent Org {ObjectId.GenerateNewId()}",
                    OrgType = OrgTypeEnum.Default
                });
                await setupUow.SaveChanges();
            }

            // Act & Assert - Create sub org using GraphQL
            var subOrgCode = $"{parentOrgCode}.suborg_{ObjectId.GenerateNewId()}";
            var subOrgName = $"Sub Organization {ObjectId.GenerateNewId()}";

            var client = this.SuperAdminClient;
            var query = """
                mutation($code: String!, $name: String!) {
                    createOrg(request: {
                        code: $code
                        name: $name
                        orgType: Default
                    }) {
                        id
                        code
                        name
                        parentOrgCode
                    }
                }
                """;

            var (responseData, responseString) = await client.PostGqlRequest(query, new { code = subOrgCode, name = subOrgName });

            var createdOrg = responseData["data"]["createOrg"];
            ((string)createdOrg["code"]).ShouldBe(subOrgCode);
            ((string)createdOrg["parentOrgCode"]).ShouldBe(parentOrgCode);
        }

        [Fact]
        public async Task DeleteOrgMutationShouldWork()
        {
            // Arrange - Prepare data using separate scope
            var testOrgCode = $"deleteapi_{ObjectId.GenerateNewId()}";
            string orgId;

            using (var scope = ScopedService.CreateScope())
            {
                var setupUow = scope.ServiceProvider.GetService<IUnitOfWork>();
                var org = await setupUow.Request(new CreateOrgRequest
                {
                    Code = testOrgCode,
                    Name = $"Delete API Test Org {ObjectId.GenerateNewId()}",
                    OrgType = OrgTypeEnum.Default
                });
                await setupUow.SaveChanges();
                orgId = org.Id;
            }

            // Act & Assert - Delete using GraphQL
            var client = this.SuperAdminClient;
            var query = """
                mutation($id: String!) {
                    deleteOrg(id: $id)
                }
                """;

            var (responseData, responseString) = await client.PostGqlRequest(query, new { id = orgId });

            bool deleteResult = (bool)responseData["data"]["deleteOrg"];
            deleteResult.ShouldBeTrue();
        }

        [Fact]
        public async Task FixUserOrgMutationShouldWork()
        {
            // Arrange & Act & Assert - GraphQL mutation
            var client = this.SuperAdminClient;
            var query = """
                mutation {
                    fixUserOrg
                }
                """;

            var (responseData, responseString) = await client.PostGqlRequest(query);

            bool fixResult = (bool)responseData["data"]["fixUserOrg"];
            fixResult.ShouldBeTrue();
        }

        [Fact]
        public async Task QueryOrgHierarchyShouldWork()
        {
            // Arrange - Prepare hierarchy data using separate scope
            var parentCode = $"parent_{ObjectId.GenerateNewId()}";
            var subCode = $"{parentCode}.sub_{ObjectId.GenerateNewId()}";

            using (var scope = ScopedService.CreateScope())
            {
                var setupUow = scope.ServiceProvider.GetService<IUnitOfWork>();
                await setupUow.Request(new CreateOrgRequest
                {
                    Code = parentCode,
                    Name = $"Parent Org {ObjectId.GenerateNewId()}",
                    OrgType = OrgTypeEnum.Default
                });
                await setupUow.Request(new CreateOrgRequest
                {
                    Code = subCode,
                    Name = $"Sub Org {ObjectId.GenerateNewId()}",
                    OrgType = OrgTypeEnum.Default
                });
                await setupUow.SaveChanges();
            }

            // Act & Assert - Query hierarchy using GraphQL
            var client = this.SuperAdminClient;
            var query = """
                query {
                    orgs(skip: 0, take: 100) {
                        items {
                            id
                            code
                            name
                            parentOrgCode
                            allParentOrgCodes
                            directSubOrgCodes
                            allSubOrgCodes
                        }
                    }
                }
                """;

            var (responseData, responseString) = await client.PostGqlRequest(query);

            var items = responseData["data"]["orgs"]["items"].AsArray();

            var parentOrgResult = items.FirstOrDefault(o => ((string)o["code"]) == parentCode);
            var subOrgResult = items.FirstOrDefault(o => ((string)o["code"]) == subCode);

            parentOrgResult.ShouldNotBeNull();
            subOrgResult.ShouldNotBeNull();

            var directSubCodes = parentOrgResult["directSubOrgCodes"].AsArray();
            directSubCodes.Any(x => ((string)x) == subCode).ShouldBeTrue();

            ((string)subOrgResult["parentOrgCode"]).ShouldBe(parentCode);
        }
    }
}
