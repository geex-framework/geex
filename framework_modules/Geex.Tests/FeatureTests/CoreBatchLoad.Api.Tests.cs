using System.Linq;

using Geex.Tests.SchemaTests.TestEntities;

using Microsoft.Extensions.DependencyInjection;

using MongoDB.Driver;
using MongoDB.Entities;

using Shouldly;

namespace Geex.Tests.FeatureTests
{
    [Collection(nameof(TestsCollection))]
    public class CoreBatchLoadApiTests : TestsBase
    {
        private const string ProfilerNamespace = "BatchLoadGraphQLEntity";

        public CoreBatchLoadApiTests(TestApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task AutoBatchLoadNestedSelectionShouldBoundDatabaseQueries()
        {
            await SeedBatchLoadDataAsync();

            await DB.RestartProfiler();

            var query = """
                query {
                  batchLoadEntities {
                    thisId
                    children {
                      thisId
                      firstChild { thisId }
                    }
                  }
                }
                """;

            var (responseData, _) = await SuperAdminClient.PostGqlRequest(query);
            responseData["data"]!["batchLoadEntities"]!.AsArray().Count.ShouldBe(5);

            DB.GetProfilerLogs().AsQueryable()
                .Count(x => x.ns != null && x.ns.Contains(ProfilerNamespace))
                .ShouldBe(3);

            DB.StopProfiler();
        }

        [Fact]
        public async Task AutoBatchLoadManualOrphanShouldKeepNestedBatchLoad()
        {
            await SeedBatchLoadDataAsync();

            await DB.RestartProfiler();

            var query = """
                query {
                  batchLoadEntitiesManualOrphan {
                    thisId
                    children { thisId firstChild { thisId } }
                  }
                }
                """;

            var (responseData, _) = await SuperAdminClient.PostGqlRequest(query);
            var root = responseData["data"]!["batchLoadEntitiesManualOrphan"]!.AsArray()
                .First(x => x!["thisId"]!.GetValue<string>() == "1");
            var child = root!["children"]!.AsArray()
                .First(x => x!["thisId"]!.GetValue<string>() == "1.1");
            child!["firstChild"]!["thisId"]!.GetValue<string>().ShouldBe("1.1.1");

            DB.GetProfilerLogs().AsQueryable()
                .Count(x => x.ns != null && x.ns.Contains(ProfilerNamespace))
                .ShouldBe(3);

            DB.StopProfiler();
        }

        [Fact]
        public async Task AutoBatchLoadManualPartialShouldBeOverwrittenBySelection()
        {
            await SeedBatchLoadDataAsync();

            await DB.RestartProfiler();

            var query = """
                query {
                  batchLoadEntitiesManualPartial {
                    thisId
                    children {
                      thisId
                      firstChild { thisId }
                    }
                  }
                }
                """;

            var (responseData, _) = await SuperAdminClient.PostGqlRequest(query);
            var root = responseData["data"]!["batchLoadEntitiesManualPartial"]!.AsArray()
                .First(x => x!["thisId"]!.GetValue<string>() == "1");
            var child = root!["children"]!.AsArray()
                .First(x => x!["thisId"]!.GetValue<string>() == "1.1");
            child!["firstChild"]!["thisId"]!.GetValue<string>().ShouldBe("1.1.1");

            DB.GetProfilerLogs().AsQueryable()
                .Count(x => x.ns != null && x.ns.Contains(ProfilerNamespace))
                .ShouldBe(3);

            DB.StopProfiler();
        }

        [Fact]
        public async Task AutoBatchLoadPagedQueryShouldBoundDatabaseQueries()
        {
            await SeedBatchLoadDataAsync();

            await DB.RestartProfiler();

            var query = """
                query {
                  batchLoadEntitiesPaged(thisId: "1") {
                    items {
                      thisId
                      children { thisId firstChild { thisId } }
                    }
                  }
                }
                """;

            var (responseData, _) = await SuperAdminClient.PostGqlRequest(query);
            responseData["data"]!["batchLoadEntitiesPaged"]!["items"]!.AsArray().Count.ShouldBe(1);

            DB.GetProfilerLogs().AsQueryable()
                .Count(x => x.ns != null && x.ns.Contains(ProfilerNamespace))
                .ShouldBe(3);

            DB.StopProfiler();
        }

        [Fact]
        public async Task AutoBatchLoadDisabledOperationShouldDegradeToNPlusOne()
        {
            await SeedBatchLoadDataAsync();

            await DB.RestartProfiler();

            var query = """
                mutation {
                  batchLoadEntitiesDisabled {
                    thisId
                    firstChild { thisId }
                  }
                }
                """;

            var (responseData, _) = await SuperAdminClient.PostGqlRequest(query);
            responseData["data"]!["batchLoadEntitiesDisabled"]!.AsArray().Count.ShouldBe(5);

            DB.GetProfilerLogs().AsQueryable()
                .Count(x => x.ns != null && x.ns.Contains(ProfilerNamespace))
                .ShouldBeGreaterThan(5);

            DB.StopProfiler();
        }

        [Fact]
        public async Task AutoBatchLoadOperationOverrideShouldEnableBatchLoadWhenGlobalDisabled()
        {
            var options = ScopedService.GetRequiredService<GeexCoreModuleOptions>();
            var original = options.AutoBatchLoad;
            options.AutoBatchLoad = false;
            try
            {
                await SeedBatchLoadDataAsync();

                await DB.RestartProfiler();

                var query = """
                    query {
                      batchLoadEntities {
                        thisId
                        firstChild { thisId }
                      }
                    }
                    """;

                var (responseData, _) = await SuperAdminClient.PostGqlRequest(query);
                responseData["data"]!["batchLoadEntities"]!.AsArray().Count.ShouldBe(5);

                DB.GetProfilerLogs().AsQueryable()
                    .Count(x => x.ns != null && x.ns.Contains(ProfilerNamespace))
                    .ShouldBe(2);

                DB.StopProfiler();
            }
            finally
            {
                options.AutoBatchLoad = original;
            }
        }

        private async Task SeedBatchLoadDataAsync()
        {
            var uow = ScopedService.GetRequiredService<IUnitOfWork>();
            await uow.DeleteAsync<BatchLoadGraphQLEntity>(_ => true);

            uow.Attach(new BatchLoadGraphQLEntity("1"));
            uow.Attach(new BatchLoadGraphQLEntity("2"));
            uow.Attach(new BatchLoadGraphQLEntity("1.1", "1"));
            uow.Attach(new BatchLoadGraphQLEntity("1.2", "1"));
            uow.Attach(new BatchLoadGraphQLEntity("2.1", "2"));
            uow.Attach(new BatchLoadGraphQLEntity("1.1.1", "1.1"));
            await uow.SaveChanges();
        }
    }
}
