using Geex.Tests.SchemaTests.TestEntities;

using Microsoft.Extensions.DependencyInjection;

using MongoDB.Entities;

using Shouldly;

namespace Geex.Tests.SchemaTests
{
    [Collection(nameof(TestsCollection))]
    public class AutoBatchLoadGraphQLTests : TestsBase
    {
        private const string NestedChildrenQuery = """
            query {
              {field} {
                thisId
                children {
                  thisId
                  firstChild { thisId }
                }
              }
            }
            """;

        public AutoBatchLoadGraphQLTests(TestApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task PagedQueryWithNestedSelectionShouldReturnNestedData()
        {
            await AutoBatchLoadTestData.SeedSingleTreeAsync(ScopedService.GetRequiredService<IUnitOfWork>());

            var query = """
                query {
                  autoBatchLoadPaged {
                    items {
                      thisId
                      children { thisId firstChild { thisId } }
                    }
                  }
                }
                """;

            var (responseData, _) = await SuperAdminClient.PostGqlRequest(query);
            var items = responseData["data"]!["autoBatchLoadPaged"]!["items"]!.AsArray();
            items.Count.ShouldBeGreaterThan(0);
            items[0]!["children"]!.AsArray().Count.ShouldBe(1);
            ((string?)items[0]!["children"]![0]!["firstChild"]!["thisId"]).ShouldBe("1.1.1");
        }

        [Fact]
        public async Task NonPagedQueryableWithNestedSelectionShouldReturnNestedData()
        {
            await AutoBatchLoadTestData.SeedSingleTreeAsync(ScopedService.GetRequiredService<IUnitOfWork>());

            var query = """
                query {
                  autoBatchLoadList {
                    thisId
                    firstChild { thisId }
                  }
                }
                """;

            var (responseData, _) = await SuperAdminClient.PostGqlRequest(query);
            var items = responseData["data"]!["autoBatchLoadList"]!.AsArray();
            items.Count.ShouldBeGreaterThan(0);
            ((string?)items[0]!["firstChild"]!["thisId"]).ShouldBe("1.1");
        }

        [Fact]
        public async Task SingleEntityQueryWithNestedSelectionShouldReturnNestedData()
        {
            await AutoBatchLoadTestData.SeedSingleTreeAsync(ScopedService.GetRequiredService<IUnitOfWork>());

            var query = """
                query {
                  autoBatchLoadById(thisId: "1") {
                    thisId
                    children { thisId firstChild { thisId } }
                  }
                }
                """;

            var (responseData, _) = await SuperAdminClient.PostGqlRequest(query);
            ((string?)responseData["data"]!["autoBatchLoadById"]!["thisId"]).ShouldBe("1");
            responseData["data"]!["autoBatchLoadById"]!["children"]!.AsArray().Count.ShouldBe(1);
        }

        [Fact]
        public async Task OptOutFieldShouldStillReturnNestedData()
        {
            await AutoBatchLoadTestData.SeedSingleTreeAsync(ScopedService.GetRequiredService<IUnitOfWork>());

            var query = """
                query {
                  autoBatchLoadOptOut(thisId: "1") {
                    thisId
                    children { thisId }
                  }
                }
                """;

            var (responseData, _) = await SuperAdminClient.PostGqlRequest(query);
            ((string?)responseData["data"]!["autoBatchLoadOptOut"]!["thisId"]).ShouldBe("1");
            responseData["data"]!["autoBatchLoadOptOut"]!["children"]!.AsArray().Count.ShouldBe(1);
        }

        [Fact]
        public async Task NestedThenBatchLoadThreeLevelsShouldReturnGrandChild()
        {
            await AutoBatchLoadTestData.SeedSingleTreeAsync(ScopedService.GetRequiredService<IUnitOfWork>());

            var query = """
                query {
                  autoBatchLoadById(thisId: "1") {
                    children {
                      firstChild { thisId }
                    }
                  }
                }
                """;

            var (responseData, _) = await SuperAdminClient.PostGqlRequest(query);
            ((string?)responseData["data"]!["autoBatchLoadById"]!["children"]![0]!["firstChild"]!["thisId"]).ShouldBe("1.1.1");
        }

        [Fact]
        public async Task ScalarOnlySelectionShouldNotBatchLoadNavigations()
        {
            await AutoBatchLoadTestData.SeedSingleTreeAsync(ScopedService.GetRequiredService<IUnitOfWork>());

            await DB.RestartProfiler();

            var query = """
                query {
                  autoBatchLoadList {
                    thisId
                  }
                }
                """;

            var (responseData, _) = await SuperAdminClient.PostGqlRequest(query);
            responseData["data"]!["autoBatchLoadList"]!.AsArray().Count.ShouldBeGreaterThan(0);

            BatchLoadProfilerAssertions.CountLogs(BatchLoadProfilerAssertions.AutoBatchLoadNamespace).ShouldBe(1);
            DB.StopProfiler();
        }

        [Fact]
        public async Task ManualBatchLoadShouldSkipAutoConfigInjection()
        {
            await AutoBatchLoadTestData.SeedMultiChildTreeAsync(ScopedService.GetRequiredService<IUnitOfWork>());

            var nestedQuery = NestedChildrenQuery.Replace("{field}", "autoBatchLoadList");
            var manualQuery = NestedChildrenQuery.Replace("{field}", "autoBatchLoadManualChildren");

            await DB.RestartProfiler();
            await SuperAdminClient.PostGqlRequest(nestedQuery);
            var autoBatchCount = BatchLoadProfilerAssertions.CountLogs(BatchLoadProfilerAssertions.AutoBatchLoadNamespace);
            DB.StopProfiler();

            await DB.RestartProfiler();
            await SuperAdminClient.PostGqlRequest(manualQuery);
            var manualBatchCount = BatchLoadProfilerAssertions.CountLogs(BatchLoadProfilerAssertions.AutoBatchLoadNamespace);
            DB.StopProfiler();

            autoBatchCount.ShouldBe(3);
            manualBatchCount.ShouldBe(4);
        }

        [Fact]
        public async Task OptOutShouldUseMoreQueriesThanAutoBatchForNestedSelection()
        {
            await AutoBatchLoadTestData.SeedMultiChildTreeAsync(ScopedService.GetRequiredService<IUnitOfWork>());

            var autoQuery = """
                query {
                  autoBatchLoadById(thisId: "1") {
                    thisId
                    children {
                      thisId
                      firstChild { thisId }
                    }
                  }
                }
                """;

            var optOutQuery = """
                query {
                  autoBatchLoadOptOut(thisId: "1") {
                    thisId
                    children {
                      thisId
                      firstChild { thisId }
                    }
                  }
                }
                """;

            await DB.RestartProfiler();
            await SuperAdminClient.PostGqlRequest(autoQuery);
            var autoBatchCount = BatchLoadProfilerAssertions.CountLogs(BatchLoadProfilerAssertions.AutoBatchLoadNamespace);
            DB.StopProfiler();

            await DB.RestartProfiler();
            await SuperAdminClient.PostGqlRequest(optOutQuery);
            var optOutCount = BatchLoadProfilerAssertions.CountLogs(BatchLoadProfilerAssertions.AutoBatchLoadNamespace);
            DB.StopProfiler();

            autoBatchCount.ShouldBe(3);
            optOutCount.ShouldBeGreaterThan(autoBatchCount);
        }

        [Fact]
        public async Task ResettableLazyNavigationShouldReturnNestedData()
        {
            await AutoBatchLoadTestData.SeedSingleTreeAsync(ScopedService.GetRequiredService<IUnitOfWork>());

            var query = """
                query {
                  autoBatchLoadById(thisId: "1") {
                    children {
                      resettableGrandChild { thisId }
                    }
                  }
                }
                """;

            var (responseData, _) = await SuperAdminClient.PostGqlRequest(query);
            ((string?)responseData["data"]!["autoBatchLoadById"]!["children"]![0]!["resettableGrandChild"]!["thisId"]).ShouldBe("1.1.1");
        }

        [Fact]
        public async Task NonCachedQueryableShouldReturnNestedDataWithoutThrowing()
        {
            await AutoBatchLoadTestData.SeedSingleTreeAsync(ScopedService.GetRequiredService<IUnitOfWork>());

            var query = """
                query {
                  autoBatchLoadNonCachedList {
                    thisId
                    children { thisId }
                  }
                }
                """;

            var (responseData, _) = await SuperAdminClient.PostGqlRequest(query);
            responseData["data"]!["autoBatchLoadNonCachedList"]!.AsArray().Count.ShouldBeGreaterThan(0);
            responseData["data"]!["autoBatchLoadNonCachedList"]![0]!["children"]!.AsArray().Count.ShouldBe(1);
        }
    }
}
