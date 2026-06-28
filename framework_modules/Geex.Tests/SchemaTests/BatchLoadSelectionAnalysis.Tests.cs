using Geex.Tests.SchemaTests.TestEntities;

using Microsoft.Extensions.DependencyInjection;

using MongoDB.Entities;

using Shouldly;

namespace Geex.Tests.SchemaTests
{
    [Collection(nameof(TestsCollection))]
    public class BatchLoadSelectionAnalysisTests : TestsBase
    {
        public BatchLoadSelectionAnalysisTests(TestApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task ListQueryWithNestedSelectionShouldBoundDatabaseQueries()
        {
            await AutoBatchLoadTestData.SeedSingleTreeAsync(ScopedService.GetRequiredService<IUnitOfWork>());

            await DB.RestartProfiler();

            var query = """
                query {
                  autoBatchLoadList {
                    thisId
                    firstChild { thisId }
                  }
                }
                """;

            var (responseData, _) = await SuperAdminClient.PostGqlRequest(query);
            responseData["data"]!["autoBatchLoadList"]!.AsArray().Count.ShouldBeGreaterThan(0);

            BatchLoadProfilerAssertions.CountLogs(BatchLoadProfilerAssertions.AutoBatchLoadNamespace).ShouldBe(2);
            DB.StopProfiler();
        }

        [Fact]
        public async Task SingleEntityQueryWithNestedSelectionShouldBoundDatabaseQueries()
        {
            await AutoBatchLoadTestData.SeedSingleTreeAsync(ScopedService.GetRequiredService<IUnitOfWork>());

            await DB.RestartProfiler();

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

            BatchLoadProfilerAssertions.CountLogs(BatchLoadProfilerAssertions.AutoBatchLoadNamespace).ShouldBe(3);
            DB.StopProfiler();
        }

        [Fact]
        public async Task PagedQueryWithNestedSelectionShouldBoundDatabaseQueries()
        {
            await AutoBatchLoadTestData.SeedSingleTreeAsync(ScopedService.GetRequiredService<IUnitOfWork>());

            await DB.RestartProfiler();

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
            responseData["data"]!["autoBatchLoadPaged"]!["items"]!.AsArray().Count.ShouldBeGreaterThan(0);

            BatchLoadProfilerAssertions.CountLogs(BatchLoadProfilerAssertions.AutoBatchLoadNamespace).ShouldBe(3);
            DB.StopProfiler();
        }

        [Fact]
        public async Task ScalarOnlySelectionShouldBoundDatabaseQueriesToRootLoad()
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
        public async Task ResettableLazySelectionShouldBoundDatabaseQueries()
        {
            await AutoBatchLoadTestData.SeedSingleTreeAsync(ScopedService.GetRequiredService<IUnitOfWork>());

            await DB.RestartProfiler();

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

            BatchLoadProfilerAssertions.CountLogs(BatchLoadProfilerAssertions.AutoBatchLoadNamespace).ShouldBe(3);
            DB.StopProfiler();
        }
    }
}
