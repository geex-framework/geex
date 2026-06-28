using System.Linq;

using Geex.Tests.SchemaTests.TestEntities;

using Microsoft.Extensions.DependencyInjection;

using MongoDB.Driver;
using MongoDB.Entities;

using Shouldly;

namespace Geex.Tests.SchemaTests
{
    [Collection(nameof(TestsCollection))]
    public class BatchLoadSelectionAnalysisTests : TestsBase
    {
        private const string ProfilerNamespaceFilter = "AutoBatchLoadTest";

        public BatchLoadSelectionAnalysisTests(TestApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task ListQueryWithNestedSelectionShouldBoundDatabaseQueries()
        {
            await SeedAsync();

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

            var logs = DB.GetProfilerLogs().AsQueryable()
                .Where(x => x.ns != null && x.ns.Contains(ProfilerNamespaceFilter));
            logs.Count().ShouldBe(2);
            DB.StopProfiler();
        }

        [Fact]
        public async Task SingleEntityQueryWithNestedSelectionShouldBoundDatabaseQueries()
        {
            await SeedAsync();

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

            var logs = DB.GetProfilerLogs().AsQueryable()
                .Where(x => x.ns != null && x.ns.Contains(ProfilerNamespaceFilter));
            logs.Count().ShouldBe(3);
            DB.StopProfiler();
        }

        [Fact]
        public async Task PagedQueryWithNestedSelectionShouldBoundDatabaseQueries()
        {
            await SeedAsync();

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

            var logs = DB.GetProfilerLogs().AsQueryable()
                .Where(x => x.ns != null && x.ns.Contains(ProfilerNamespaceFilter));
            logs.Count().ShouldBe(3);
            DB.StopProfiler();
        }

        [Fact]
        public async Task ScalarOnlySelectionShouldBoundDatabaseQueriesToRootLoad()
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
                .Where(x => x.ns != null && x.ns.Contains(ProfilerNamespaceFilter));
            logs.Count().ShouldBe(1);
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
