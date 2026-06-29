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
        private const string ProfilerNamespaceFilter = "AutoBatchLoad";

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
            await SeedSingleTreeAsync();

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
            await SeedSingleTreeAsync();

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
            await SeedSingleTreeAsync();

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
            await SeedSingleTreeAsync();

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
            await SeedSingleTreeAsync();

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
            await SeedSingleTreeAsync();

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

            CountProfilerLogs().ShouldBe(1);
            DB.StopProfiler();
        }

        [Fact]
        public async Task ManualBatchLoadShouldSkipAutoConfigInjection()
        {
            await SeedMultiChildTreeAsync();

            var nestedQuery = NestedChildrenQuery.Replace("{field}", "autoBatchLoadList");
            var manualQuery = NestedChildrenQuery.Replace("{field}", "autoBatchLoadManualChildren");

            await DB.RestartProfiler();
            await SuperAdminClient.PostGqlRequest(nestedQuery);
            var autoBatchCount = CountProfilerLogs();
            DB.StopProfiler();

            await DB.RestartProfiler();
            await SuperAdminClient.PostGqlRequest(manualQuery);
            var manualBatchCount = CountProfilerLogs();
            DB.StopProfiler();

            autoBatchCount.ShouldBe(3);
            manualBatchCount.ShouldBe(4);
        }

        [Fact]
        public async Task OptOutShouldUseMoreQueriesThanAutoBatchForNestedSelection()
        {
            await SeedMultiChildTreeAsync();

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
            var autoBatchCount = CountProfilerLogs();
            DB.StopProfiler();

            await DB.RestartProfiler();
            await SuperAdminClient.PostGqlRequest(optOutQuery);
            var optOutCount = CountProfilerLogs();
            DB.StopProfiler();

            autoBatchCount.ShouldBe(3);
            optOutCount.ShouldBeGreaterThan(autoBatchCount);
        }

        [Fact]
        public async Task ResettableLazyNavigationShouldReturnNestedData()
        {
            await SeedSingleTreeAsync();

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
            await SeedSingleTreeAsync();

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

        [Fact]
        public async Task ListQueryWithNestedSelectionShouldBoundDatabaseQueries()
        {
            await SeedSingleTreeAsync();

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

            CountProfilerLogs().ShouldBe(2);
            DB.StopProfiler();
        }

        [Fact]
        public async Task SingleEntityQueryWithNestedSelectionShouldBoundDatabaseQueries()
        {
            await SeedSingleTreeAsync();

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

            CountProfilerLogs().ShouldBe(3);
            DB.StopProfiler();
        }

        [Fact]
        public async Task PagedQueryWithNestedSelectionShouldBoundDatabaseQueries()
        {
            await SeedSingleTreeAsync();

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

            CountProfilerLogs().ShouldBe(3);
            DB.StopProfiler();
        }

        [Fact]
        public async Task ScalarOnlySelectionShouldBoundDatabaseQueriesToRootLoad()
        {
            await SeedSingleTreeAsync();

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

            CountProfilerLogs().ShouldBe(1);
            DB.StopProfiler();
        }

        [Fact]
        public async Task ResettableLazySelectionShouldBoundDatabaseQueries()
        {
            await SeedSingleTreeAsync();

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

            CountProfilerLogs().ShouldBe(3);
            DB.StopProfiler();
        }

        private int CountProfilerLogs() =>
            DB.GetProfilerLogs().AsQueryable()
                .Count(x => x.ns != null && x.ns.Contains(ProfilerNamespaceFilter));

        private async Task SeedSingleTreeAsync()
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

        private async Task SeedMultiChildTreeAsync()
        {
            var uow = ScopedService.GetRequiredService<IUnitOfWork>();
            await uow.DeleteAsync<AutoBatchLoadTestEntity>(_ => true);
            await uow.DeleteAsync<AutoBatchLoadChildEntity>(_ => true);
            await uow.DeleteAsync<AutoBatchLoadGrandChildEntity>(_ => true);

            uow.Attach(new AutoBatchLoadTestEntity("1"));
            uow.Attach(new AutoBatchLoadChildEntity("1.1", "1"));
            uow.Attach(new AutoBatchLoadChildEntity("1.2", "1"));
            uow.Attach(new AutoBatchLoadGrandChildEntity("1.1.1", "1.1"));
            uow.Attach(new AutoBatchLoadGrandChildEntity("1.2.1", "1.2"));
            await uow.SaveChanges();
        }
    }
}
