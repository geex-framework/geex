using Geex.Extensions.Requests.MultiTenant;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Geex.Abstractions;
using Geex.Extensions.MultiTenant.Core.Aggregates.Tenants;
using Geex.MultiTenant;
using MongoDB.Bson;
using System.Text.Json.Nodes;

namespace Geex.Tests.FeatureTests
{
    [Collection(nameof(TestsCollection))]
    public class MultiTenantServiceTests : TestsBase
    {
        public MultiTenantServiceTests(TestApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task CreateTenantServiceShouldWork()
        {
            // Arrange
            var testTenantCode = $"test_tenant_{ObjectId.GenerateNewId()}";
            var testTenantName = "Test Tenant";

            // Act
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetService<IUnitOfWork>();
                await uow.DbContext.Query<Tenant>().Where(x => x.Code == testTenantCode).DeleteAsync();

                var request = new CreateTenantRequest
                {
                    Code = testTenantCode,
                    Name = testTenantName,
                    ExternalInfo = JsonNode.Parse("{\"source\":\"test\"}")
                };

                var tenant = await uow.Request(request);
                await uow.SaveChanges();

                // Assert
                tenant.ShouldNotBeNull();
                tenant.Code.ShouldBe(testTenantCode);
                tenant.Name.ShouldBe(testTenantName);
                tenant.IsEnabled.ShouldBe(true);
                tenant.ExternalInfo.ShouldNotBeNull();
            }
        }

        [Fact]
        public async Task EditTenantServiceShouldWork()
        {
            // Arrange
            var testTenantCode = $"edit_tenant_{ObjectId.GenerateNewId()}";
            var originalName = "Original Tenant";
            var updatedName = "Updated Tenant";

            // Create tenant
            using (var setupScope = ScopedService.CreateScope())
            {
                var setupUow = setupScope.ServiceProvider.GetService<IUnitOfWork>();
                await setupUow.DbContext.Query<Tenant>().Where(x => x.Code == testTenantCode).DeleteAsync();

                var createRequest = new CreateTenantRequest
                {
                    Code = testTenantCode,
                    Name = originalName,
                    ExternalInfo = null
                };

                await setupUow.Request(createRequest);
                await setupUow.SaveChanges();
            }

            // Act - Edit tenant
            using (var editScope = ScopedService.CreateScope())
            {
                var editUow = editScope.ServiceProvider.GetService<IUnitOfWork>();

                var editRequest = new EditTenantRequest
                {
                    Code = testTenantCode,
                    Name = updatedName
                };

                var editedTenant = await editUow.Request(editRequest);
                await editUow.SaveChanges();

                // Assert
                editedTenant.Code.ShouldBe(testTenantCode);
                editedTenant.Name.ShouldBe(updatedName);
            }
        }

        [Fact]
        public async Task ToggleTenantAvailabilityServiceShouldWork()
        {
            // Arrange
            var testTenantCode = $"toggle_tenant_{ObjectId.GenerateNewId()}";

            // Create tenant
            using (var setupScope = ScopedService.CreateScope())
            {
                var setupUow = setupScope.ServiceProvider.GetService<IUnitOfWork>();
                await setupUow.DbContext.Query<Tenant>().Where(x => x.Code == testTenantCode).DeleteAsync();

                var createRequest = new CreateTenantRequest
                {
                    Code = testTenantCode,
                    Name = "Toggle Test Tenant",
                    ExternalInfo = null
                };

                await setupUow.Request(createRequest);
                await setupUow.SaveChanges();
            }

            // Act - Toggle availability
            using (var toggleScope = ScopedService.CreateScope())
            {
                var toggleUow = toggleScope.ServiceProvider.GetService<IUnitOfWork>();

                var toggleRequest = new ToggleTenantAvailabilityRequest
                {
                    Code = testTenantCode
                };

                var isEnabled = await toggleUow.Request(toggleRequest);
                await toggleUow.SaveChanges();

                // Assert - Should be disabled now (originally enabled)
                isEnabled.ShouldBe(false);

                // Toggle again
                var isEnabledAgain = await toggleUow.Request(toggleRequest);
                await toggleUow.SaveChanges();

                // Assert - Should be enabled again
                isEnabledAgain.ShouldBe(true);
            }
        }

        [Fact]
        public async Task CreateTenantWithDuplicateCodeShouldThrow()
        {
            // Arrange
            var testTenantCode = $"duplicate_tenant_{ObjectId.GenerateNewId()}";

            // Create first tenant
            using (var setupScope = ScopedService.CreateScope())
            {
                var setupUow = setupScope.ServiceProvider.GetService<IUnitOfWork>();
                await setupUow.DbContext.Query<Tenant>().Where(x => x.Code == testTenantCode).DeleteAsync();

                var request = new CreateTenantRequest
                {
                    Code = testTenantCode,
                    Name = "First Tenant",
                    ExternalInfo = null
                };

                await setupUow.Request(request);
                await setupUow.SaveChanges();
            }

            // Act & Assert - Try to create duplicate
            using (var duplicateScope = ScopedService.CreateScope())
            {
                var duplicateUow = duplicateScope.ServiceProvider.GetService<IUnitOfWork>();

                var duplicateRequest = new CreateTenantRequest
                {
                    Code = testTenantCode,
                    Name = "Duplicate Tenant",
                    ExternalInfo = null
                };

                Should.Throw<Exception>(async () =>
                {
                    await duplicateUow.Request(duplicateRequest);
                    await duplicateUow.SaveChanges();
                });
            }
        }

        [Fact]
        public async Task EditNonExistentTenantShouldThrow()
        {
            // Arrange
            var nonExistentCode = $"nonexistent_{ObjectId.GenerateNewId()}";

            // Act & Assert
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetService<IUnitOfWork>();

                var editRequest = new EditTenantRequest
                {
                    Code = nonExistentCode,
                    Name = "Updated Name"
                };

                Should.Throw<BusinessException>(async () =>
                    await uow.Request(editRequest));
            }
        }

        [Fact]
        public async Task ToggleNonExistentTenantShouldThrow()
        {
            // Arrange
            var nonExistentCode = $"nonexistent_toggle_{ObjectId.GenerateNewId()}";

            // Act & Assert
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetService<IUnitOfWork>();

                var toggleRequest = new ToggleTenantAvailabilityRequest
                {
                    Code = nonExistentCode
                };

                Should.Throw<BusinessException>(async () =>
                    await uow.Request(toggleRequest));
            }
        }

        [Fact]
        public async Task CurrentTenantShouldWork()
        {
            // Arrange
            var testTenantCode = $"current_tenant_{ObjectId.GenerateNewId()}";

            // Create tenant
            using (var setupScope = ScopedService.CreateScope())
            {
                var setupUow = setupScope.ServiceProvider.GetService<IUnitOfWork>();
                await setupUow.DbContext.Query<Tenant>().Where(x => x.Code == testTenantCode).DeleteAsync();

                var request = new CreateTenantRequest
                {
                    Code = testTenantCode,
                    Name = "Current Tenant Test",
                    ExternalInfo = null
                };

                await setupUow.Request(request);
                await setupUow.SaveChanges();
            }

            // Act & Assert
            using (var scope = ScopedService.CreateScope())
            {
                var currentTenant = scope.ServiceProvider.GetService<ICurrentTenant>();

                using (currentTenant.Change(testTenantCode))
                {
                    currentTenant.Code.ShouldBe(testTenantCode);
                    currentTenant.Detail.ShouldNotBeNull();
                    currentTenant.Detail.Code.ShouldBe(testTenantCode);
                }
            }
        }
    }
}
