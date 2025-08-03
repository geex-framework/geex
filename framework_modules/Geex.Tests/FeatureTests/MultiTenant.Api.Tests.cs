using Geex.Extensions.Requests.MultiTenant;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Geex.Abstractions;
using Geex.Extensions.MultiTenant.Core.Aggregates.Tenants;
using MongoDB.Bson;

namespace Geex.Tests.FeatureTests
{
    [Collection(nameof(TestsCollection))]
    public class MultiTenantApiTests : TestsBase
    {
        public MultiTenantApiTests(TestApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task QueryTenantsShouldWork()
        {
            // Arrange
            var client = this.SuperAdminClient;
            var query = """
                query {
                    tenants(skip: 0, take: 10) {
                        items {
                            code
                            name
                            isEnabled
                            externalInfo
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
            var (responseData, responseString) = await client.PostGqlRequest(query);

            // Assert
            responseData["data"]["tenants"]["totalCount"].GetValue<int>().ShouldBeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public async Task FilterTenantsByCodeShouldWork()
        {
            // Arrange
            var client = this.SuperAdminClient;
            var targetTenantCode = $"filter_test_{ObjectId.GenerateNewId()}";

            // Prepare data using separate scope
            using (var scope = ScopedService.CreateScope())
            {
                var setupUow = scope.ServiceProvider.GetService<IUnitOfWork>();
                await setupUow.DbContext.Query<Tenant>().Where(x => x.Code == targetTenantCode).DeleteAsync();
                await setupUow.Request(new CreateTenantRequest
                {
                    Code = targetTenantCode,
                    Name = "Filter Test Tenant",
                    ExternalInfo = null
                });
                await setupUow.SaveChanges();
            }

            var query = """
                query($code: String!) {
                    tenants(skip: 0, take: 10, filter: { code: { eq: $code }}) {
                        items {
                            code
                            name
                            isEnabled
                        }
                        totalCount
                    }
                }
                """;

            // Act
            var (responseData, responseString) = await client.PostGqlRequest(query, new { code = targetTenantCode });

            // Assert
            var items = responseData["data"]["tenants"]["items"].AsArray();
            items.Count.ShouldBeGreaterThan(0);
            ((string)items[0]["code"]).ShouldBe(targetTenantCode);
        }

        [Fact]
        public async Task CreateTenantMutationShouldWork()
        {
            // Arrange
            var client = this.SuperAdminClient;
            var testTenantCode = $"create_api_{ObjectId.GenerateNewId()}";

            var query = """
                mutation($code: String!, $name: String!) {
                    createTenant(request: {
                        code: $code
                        name: $name
                        externalInfo: "{\"source\":\"api_test\"}"
                    }) {
                        code
                        name
                        isEnabled
                        externalInfo
                    }
                }
                """;

            // Act
            var (responseData, responseString) = await client.PostGqlRequest(query, new { code = testTenantCode, name = "API Created Tenant" });

            // Assert
            var createdTenant = responseData["data"]["createTenant"];
            ((string)createdTenant["code"]).ShouldBe(testTenantCode);
            ((string)createdTenant["name"]).ShouldBe("API Created Tenant");
            ((bool)createdTenant["isEnabled"]).ShouldBe(true);
            createdTenant["externalInfo"].ShouldNotBeNull();
        }

        [Fact]
        public async Task EditTenantMutationShouldWork()
        {
            // Arrange
            var client = this.SuperAdminClient;
            var testTenantCode = $"edit_api_{ObjectId.GenerateNewId()}";

            // Prepare data using separate scope
            using (var scope = ScopedService.CreateScope())
            {
                var setupUow = scope.ServiceProvider.GetService<IUnitOfWork>();
                await setupUow.DbContext.Query<Tenant>().Where(x => x.Code == testTenantCode).DeleteAsync();
                await setupUow.Request(new CreateTenantRequest
                {
                    Code = testTenantCode,
                    Name = "Original API Tenant",
                    ExternalInfo = null
                });
                await setupUow.SaveChanges();
            }

            var query = """
                mutation($code: String!, $name: String!) {
                    editTenant(request: {
                        code: $code
                        name: $name
                    }) {
                        code
                        name
                        isEnabled
                    }
                }
                """;

            // Act
            var (responseData, responseString) = await client.PostGqlRequest(query, new { code = testTenantCode, name = "Updated API Tenant" });

            // Assert
            var editedTenant = responseData["data"]["editTenant"];
            ((string)editedTenant["code"]).ShouldBe(testTenantCode);
            ((string)editedTenant["name"]).ShouldBe("Updated API Tenant");
        }

        [Fact]
        public async Task ToggleTenantAvailabilityMutationShouldWork()
        {
            // Arrange
            var client = this.SuperAdminClient;
            var testTenantCode = $"toggle_api_{ObjectId.GenerateNewId()}";

            // Prepare data using separate scope
            using (var scope = ScopedService.CreateScope())
            {
                var setupUow = scope.ServiceProvider.GetService<IUnitOfWork>();
                await setupUow.DbContext.Query<Tenant>().Where(x => x.Code == testTenantCode).DeleteAsync();
                await setupUow.Request(new CreateTenantRequest
                {
                    Code = testTenantCode,
                    Name = "Toggle API Tenant",
                    ExternalInfo = null
                });
                await setupUow.SaveChanges();
            }

            var query = """
                mutation($code: String!) {
                    toggleTenantAvailability(request: { code: $code })
                }
                """;

            // Act - First toggle (should disable)
            var (responseData1, responseString1) = await client.PostGqlRequest(query, new { code = testTenantCode });

            // Assert - First toggle
            bool firstToggleResult = (bool)responseData1["data"]["toggleTenantAvailability"];
            firstToggleResult.ShouldBe(false);

            // Act - Second toggle (should enable)
            var (responseData2, responseString2) = await client.PostGqlRequest(query, new { code = testTenantCode });

            // Assert - Second toggle
            bool secondToggleResult = (bool)responseData2["data"]["toggleTenantAvailability"];
            secondToggleResult.ShouldBe(true);
        }

        [Fact]
        public async Task CheckTenantMutationShouldWork()
        {
            // Arrange
            var client = this.SuperAdminClient;
            var testTenantCode = $"check_api_{ObjectId.GenerateNewId()}";

            // Prepare data using separate scope
            using (var scope = ScopedService.CreateScope())
            {
                var setupUow = scope.ServiceProvider.GetService<IUnitOfWork>();
                await setupUow.DbContext.Query<Tenant>().Where(x => x.Code == testTenantCode).DeleteAsync();
                await setupUow.Request(new CreateTenantRequest
                {
                    Code = testTenantCode,
                    Name = "Check API Tenant",
                    ExternalInfo = null
                });
                await setupUow.SaveChanges();
            }

            var query = """
                mutation($code: String!) {
                    checkTenant(code: $code) {
                        code
                        name
                        isEnabled
                    }
                }
                """;

            // Act
            var (responseData, responseString) = await client.PostGqlRequest(query, new { code = testTenantCode });

            // Assert
            var checkedTenant = responseData["data"]["checkTenant"];
            checkedTenant.ShouldNotBeNull();
            ((string)checkedTenant["code"]).ShouldBe(testTenantCode);
            ((string)checkedTenant["name"]).ShouldBe("Check API Tenant");
        }

        [Fact]
        public async Task CheckNonExistentTenantShouldReturnNull()
        {
            // Arrange
            var client = this.SuperAdminClient;
            var nonExistentCode = $"nonexistent_{ObjectId.GenerateNewId()}";

            var query = """
                mutation($code: String!) {
                    checkTenant(code: $code) {
                        code
                        name
                    }
                }
                """;

            // Act
            var (responseData, responseString) = await client.PostGqlRequest(query, new { code = nonExistentCode });

            // Assert
            responseData["data"]["checkTenant"].ShouldBeNull();
        }

        [Fact]
        public async Task FilterTenantsByIsEnabledShouldWork()
        {
            // Arrange
            var client = this.SuperAdminClient;
            var query = """
                query {
                    tenants(skip: 0, take: 10, filter: { isEnabled: { eq: true } }) {
                        items {
                            code
                            name
                            isEnabled
                        }
                        totalCount
                    }
                }
                """;

            // Act
            var (responseData, responseString) = await client.PostGqlRequest(query);

            // Assert
            var items = responseData["data"]["tenants"]["items"].AsArray();
            foreach (var item in items)
            {
                ((bool)item["isEnabled"]).ShouldBe(true);
            }
        }

        [Fact]
        public async Task CreateTenantWithInvalidDataShouldFail()
        {
            // Arrange
            var client = this.SuperAdminClient;

            var query = """
                mutation {
                    createTenant(request: {
                        code: ""
                        name: ""
                    }) {
                        code
                        name
                    }
                }
                """;

            // Act & Assert
            var (responseData, responseString) = await client.PostGqlRequest(query, ignoreError: true);
            responseData["errors"].ShouldNotBeNull();
        }
    }
}
