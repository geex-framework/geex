using System.Linq;

using Geex.Tests.FeatureTests.TestEntities;

using Microsoft.Extensions.DependencyInjection;

using MongoDB.Driver;
using MongoDB.Entities;

using Shouldly;

namespace Geex.Tests.FeatureTests
{
    [Collection(nameof(TestsCollection))]
    public class CoreBatchLoadApiTests : TestsBase
    {
        public CoreBatchLoadApiTests(TestApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task AutoBatchLoadPagedQueryShouldReturnNestedData()
        {
            await SeedBatchLoadDataAsync();

            var query = """
                query {
                  coreBatchLoadPaged(thisId: "1") {
                    items {
                      thisId
                      children { thisId firstChild { thisId } }
                    }
                  }
                }
                """;

            var (responseData, _) = await SuperAdminClient.PostGqlRequest(query);
            var items = responseData["data"]!["coreBatchLoadPaged"]!["items"]!.AsArray();
            items.Count.ShouldBe(1);
            items[0]!["children"]!.AsArray().Count.ShouldBe(2);
            var pagedChild = items[0]!["children"]!.AsArray()
                .First(x => x!["thisId"]!.GetValue<string>() == "1.1");
            ((string?)pagedChild!["firstChild"]!["thisId"]).ShouldBe("1.1.1");
        }

        [Fact]
        public async Task AutoBatchLoadListQueryShouldReturnNestedData()
        {
            await SeedBatchLoadDataAsync();

            var query = """
                query {
                  coreBatchLoadList {
                    thisId
                    firstChild { thisId }
                  }
                }
                """;

            var (responseData, _) = await SuperAdminClient.PostGqlRequest(query);
            var items = responseData["data"]!["coreBatchLoadList"]!.AsArray();
            var root = items.First(x => x!["thisId"]!.GetValue<string>() == "1");
            ((string?)root!["firstChild"]!["thisId"]).ShouldBe("1.1");
        }

        [Fact]
        public async Task AutoBatchLoadByIdShouldReturnNestedData()
        {
            await SeedBatchLoadDataAsync();

            var query = """
                query {
                  coreBatchLoadById(thisId: "1") {
                    thisId
                    children { thisId firstChild { thisId } }
                  }
                }
                """;

            var (responseData, _) = await SuperAdminClient.PostGqlRequest(query);
            ((string?)responseData["data"]!["coreBatchLoadById"]!["thisId"]).ShouldBe("1");
            responseData["data"]!["coreBatchLoadById"]!["children"]!.AsArray().Count.ShouldBe(2);
            var child = responseData["data"]!["coreBatchLoadById"]!["children"]!.AsArray()
                .First(x => x!["thisId"]!.GetValue<string>() == "1.1");
            ((string?)child!["firstChild"]!["thisId"]).ShouldBe("1.1.1");
        }

        [Fact]
        public async Task AutoBatchLoadOptOutShouldStillReturnNestedData()
        {
            await SeedBatchLoadDataAsync();

            var query = """
                query {
                  coreBatchLoadOptOut(thisId: "1") {
                    thisId
                    children { thisId }
                  }
                }
                """;

            var (responseData, _) = await SuperAdminClient.PostGqlRequest(query);
            ((string?)responseData["data"]!["coreBatchLoadOptOut"]!["thisId"]).ShouldBe("1");
            responseData["data"]!["coreBatchLoadOptOut"]!["children"]!.AsArray().Count.ShouldBe(2);
        }

        [Fact]
        public async Task AutoBatchLoadNestedThreeLevelsShouldReturnGrandChild()
        {
            await SeedBatchLoadDataAsync();

            var query = """
                query {
                  coreBatchLoadById(thisId: "1") {
                    children {
                      thisId
                      firstChild { thisId }
                    }
                  }
                }
                """;

            var (responseData, _) = await SuperAdminClient.PostGqlRequest(query);
            var child = responseData["data"]!["coreBatchLoadById"]!["children"]!.AsArray()
                .First(x => x!["thisId"]!.GetValue<string>() == "1.1");
            ((string?)child!["firstChild"]!["thisId"]).ShouldBe("1.1.1");
        }

        [Fact]
        public async Task AutoBatchLoadListShouldBoundDatabaseQueries()
        {
            await SeedBatchLoadDataAsync();

            await DB.RestartProfiler();

            var query = """
                query {
                  coreBatchLoadList {
                    thisId
                    firstChild { thisId }
                  }
                }
                """;

            var (responseData, _) = await SuperAdminClient.PostGqlRequest(query);
            responseData["data"]!["coreBatchLoadList"]!.AsArray().Count.ShouldBe(2);

            BatchLoadProfilerAssertions.CountLogs(BatchLoadProfilerAssertions.BatchLoadTestNamespace).ShouldBe(2);
            DB.StopProfiler();
        }

        [Fact]
        public async Task AutoBatchLoadByIdShouldBoundDatabaseQueries()
        {
            await SeedBatchLoadDataAsync();

            await DB.RestartProfiler();

            var query = """
                query {
                  coreBatchLoadById(thisId: "1") {
                    thisId
                    children { thisId firstChild { thisId } }
                  }
                }
                """;

            var (responseData, _) = await SuperAdminClient.PostGqlRequest(query);
            ((string?)responseData["data"]!["coreBatchLoadById"]!["thisId"]).ShouldBe("1");

            BatchLoadProfilerAssertions.CountLogs(BatchLoadProfilerAssertions.BatchLoadTestNamespace).ShouldBe(3);
            DB.StopProfiler();
        }

        private async Task SeedBatchLoadDataAsync()
        {
            var uow = ScopedService.GetRequiredService<IUnitOfWork>();
            await uow.DeleteAsync<BatchLoadTestEntity>(_ => true);
            await uow.DeleteAsync<BatchLoadTestChildEntity>(_ => true);
            await uow.DeleteAsync<BatchLoadTestGrandChildEntity>(_ => true);

            uow.Attach(new BatchLoadTestEntity("1"));
            uow.Attach(new BatchLoadTestEntity("2"));
            uow.Attach(new BatchLoadTestChildEntity("1.1", "1"));
            uow.Attach(new BatchLoadTestChildEntity("1.2", "1"));
            uow.Attach(new BatchLoadTestChildEntity("2.1", "2"));
            uow.Attach(new BatchLoadTestGrandChildEntity("1.1.1", "1.1"));
            await uow.SaveChanges();
        }
    }
}
