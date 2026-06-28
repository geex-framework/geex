using System.Linq;

using Geex.Tests.SchemaTests.TestEntities;

using Microsoft.Extensions.DependencyInjection;

using MongoDB.Driver;
using MongoDB.Entities;

using Shouldly;

namespace Geex.Tests.SchemaTests
{
    [Collection(nameof(TestsCollection))]
    public class AutoBatchLoadGraphQLTests : TestsBase
    {
        public AutoBatchLoadGraphQLTests(TestApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task PagedQueryWithNestedSelectionShouldReturnNestedData()
        {
            await SeedAsync();

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
            await SeedAsync();

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
            await SeedAsync();

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
            await SeedAsync();

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
            await SeedAsync();

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
            await SeedAsync();

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

            var logs = DB.GetProfilerLogs().AsQueryable()
                .Where(x => x.ns != null && x.ns.Contains("AutoBatchLoadTest"));
            logs.Count().ShouldBe(1);
            DB.StopProfiler();
        }

        [Fact]
        public async Task ManualBatchLoadShouldSkipAutoConfigInjection()
        {
            await SeedAsync();

            await DB.RestartProfiler();

            var query = """
                query {
                  autoBatchLoadManualChildren {
                    thisId
                    children { thisId firstChild { thisId } }
                  }
                }
                """;

            var (responseData, _) = await SuperAdminClient.PostGqlRequest(query);
            responseData["data"]!["autoBatchLoadManualChildren"]!.AsArray().Count.ShouldBeGreaterThan(0);

            var logs = DB.GetProfilerLogs().AsQueryable()
                .Where(x => x.ns != null && x.ns.Contains("AutoBatchLoadTest"));
            logs.Count().ShouldBe(3);
            DB.StopProfiler();
        }

        private async Task SeedAsync()
        {
            var uow = ScopedService.GetRequiredService<IUnitOfWork>();
            await uow.DeleteAsync<AutoBatchLoadTestEntity>(_ => true);
            await uow.DeleteAsync<AutoBatchLoadChildEntity>(_ => true);
            await uow.DeleteAsync<AutoBatchLoadGrandChildEntity>(_ => true);

            uow.Attach(new AutoBatchLoadTestEntity("1"));
            uow.Attach(new AutoBatchLoadChildEntity("1.1", "1"));
            uow.Attach(new AutoBatchLoadGrandChildEntity("1.1.1", "1.1"));
            await uow.SaveChanges();
        }
    }
}
