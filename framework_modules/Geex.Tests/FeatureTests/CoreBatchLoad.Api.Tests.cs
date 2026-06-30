using System.Linq;
using System.Threading.Tasks;

using Geex.Tests.SchemaTests.TestEntities;

using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;

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
                    childNodes {
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
                    childNodes { thisId firstChild { thisId } }
                  }
                }
                """;

            var (responseData, _) = await SuperAdminClient.PostGqlRequest(query);
            var root = responseData["data"]!["batchLoadEntitiesManualOrphan"]!.AsArray()
                .First(x => x!["thisId"]!.GetValue<string>() == "1");
            var child = root!["childNodes"]!.AsArray()
                .First(x => x!["thisId"]!.GetValue<string>() == "1.1");
            child!["firstChild"]!["thisId"]!.GetValue<string>().ShouldBe("1.1.1");

            DB.GetProfilerLogs().AsQueryable()
                .Count(x => x.ns != null && x.ns.Contains(ProfilerNamespace))
                .ShouldBe(3);

            DB.StopProfiler();
        }

        [Fact]
        public async Task AutoBatchLoadManualPartialShouldBeSupplementedBySelection()
        {
            await SeedBatchLoadDataAsync();

            await DB.RestartProfiler();

            var query = """
                query {
                  batchLoadEntitiesManualPartial {
                    thisId
                    childNodes {
                      thisId
                      firstChild { thisId }
                    }
                  }
                }
                """;

            var (responseData, _) = await SuperAdminClient.PostGqlRequest(query);
            var root = responseData["data"]!["batchLoadEntitiesManualPartial"]!.AsArray()
                .First(x => x!["thisId"]!.GetValue<string>() == "1");
            var child = root!["childNodes"]!.AsArray()
                .First(x => x!["thisId"]!.GetValue<string>() == "1.1");
            child!["firstChild"]!["thisId"]!.GetValue<string>().ShouldBe("1.1.1");

            DB.GetProfilerLogs().AsQueryable()
                .Count(x => x.ns != null && x.ns.Contains(ProfilerNamespace))
                .ShouldBe(3);

            DB.StopProfiler();
        }

        [Fact]
        public async Task AutoBatchLoadManualOnlyShouldBePreservedWhenSelectionHasNoNavigation()
        {
            await SeedBatchLoadDataAsync();

            await DB.RestartProfiler();

            var query = """
                query {
                  batchLoadEntitiesManualOnly {
                    thisId
                  }
                }
                """;

            var (responseData, _) = await SuperAdminClient.PostGqlRequest(query);
            responseData["data"]!["batchLoadEntitiesManualOnly"]!.AsArray().Count.ShouldBe(5);

            DB.GetProfilerLogs().AsQueryable()
                .Count(x => x.ns != null && x.ns.Contains(ProfilerNamespace))
                .ShouldBe(2);

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
                      childNodes { thisId firstChild { thisId } }
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
        public async Task AutoBatchLoadPagedWithTotalCountShouldNotDoubleSubQueries()
        {
            await SeedBatchLoadDataAsync();

            await DB.RestartProfiler();

            var query = """
                query {
                  batchLoadEntitiesPaged(thisId: "1") {
                    totalCount
                    items {
                      thisId
                      childNodes { thisId firstChild { thisId } }
                    }
                  }
                }
                """;

            var (responseData, _) = await SuperAdminClient.PostGqlRequest(query);
            responseData["data"]!["batchLoadEntitiesPaged"]!["totalCount"]!.GetValue<int>().ShouldBe(1);
            responseData["data"]!["batchLoadEntitiesPaged"]!["items"]!.AsArray().Count.ShouldBe(1);
            var child = responseData["data"]!["batchLoadEntitiesPaged"]!["items"]!.AsArray()[0]!["childNodes"]!.AsArray()
                .First(x => x!["thisId"]!.GetValue<string>() == "1.1");
            child!["firstChild"]!["thisId"]!.GetValue<string>().ShouldBe("1.1.1");

            var queryCount = DB.GetProfilerLogs().AsQueryable()
                .Count(x => x.ns != null && x.ns.Contains(ProfilerNamespace));
            queryCount.ShouldBeGreaterThanOrEqualTo(3);
            queryCount.ShouldBeLessThanOrEqualTo(6);

            DB.StopProfiler();
        }

        [Fact]
        public async Task AutoBatchLoadInterfacePagedQueryShouldBoundDatabaseQueries()
        {
            await SeedBatchLoadDataAsync();

            await DB.RestartProfiler();

            var query = """
                query {
                  batchLoadInterfaceEntitiesPaged(thisId: "1") {
                    items {
                      thisId
                    }
                  }
                }
                """;

            var (responseData, _) = await SuperAdminClient.PostGqlRequest(query);
            responseData["data"]!["batchLoadInterfaceEntitiesPaged"]!["items"]!.AsArray().Count.ShouldBe(1);

            DB.GetProfilerLogs().AsQueryable()
                .Count(x => x.ns != null && x.ns.Contains(ProfilerNamespace))
                .ShouldBe(1);

            DB.StopProfiler();
        }

        [Fact]
        public async Task AutoBatchLoadFilteredQueryShouldBoundDatabaseQueries()
        {
            await SeedBatchLoadDataAsync();

            await DB.RestartProfiler();

            var query = """
                query {
                  batchLoadEntitiesFiltered(thisId: "1") {
                    thisId
                    childNodes { thisId firstChild { thisId } }
                  }
                }
                """;

            var (responseData, _) = await SuperAdminClient.PostGqlRequest(query);
            responseData["data"]!["batchLoadEntitiesFiltered"]!.AsArray().Count.ShouldBe(1);

            DB.GetProfilerLogs().AsQueryable()
                .Count(x => x.ns != null && x.ns.Contains(ProfilerNamespace))
                .ShouldBe(3);

            DB.StopProfiler();
        }

        [Fact]
        public async Task AutoBatchLoadSkipDirectiveShouldNotBatchLoadSkippedNavigation()
        {
            await SeedBatchLoadDataAsync();

            await DB.RestartProfiler();

            var query = """
                query($skipChildren: Boolean!) {
                  batchLoadEntities {
                    thisId
                    childNodes @skip(if: $skipChildren) {
                      thisId
                      firstChild { thisId }
                    }
                  }
                }
                """;

            var (responseData, _) = await SuperAdminClient.PostGqlRequest(
                query,
                new { skipChildren = true });
            responseData["data"]!["batchLoadEntities"]!.AsArray().Count.ShouldBe(5);

            DB.GetProfilerLogs().AsQueryable()
                .Count(x => x.ns != null && x.ns.Contains(ProfilerNamespace))
                .ShouldBe(1);

            DB.StopProfiler();
        }

        [Fact]
        public async Task AutoBatchLoadFragmentShouldBatchLoadNestedNavigation()
        {
            await SeedBatchLoadDataAsync();

            await DB.RestartProfiler();

            var query = """
                query {
                  batchLoadEntities {
                    ...BatchLoadEntityFields
                  }
                }
                fragment BatchLoadEntityFields on BatchLoadGraphQLEntity {
                  thisId
                  childNodes {
                    thisId
                    firstChild { thisId }
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
        public async Task AutoBatchLoadRenamedNavigationFieldShouldBatchLoad()
        {
            await SeedBatchLoadDataAsync();

            await DB.RestartProfiler();

            var query = """
                query {
                  batchLoadEntities {
                    thisId
                    childNodes {
                      thisId
                      firstChild { thisId }
                    }
                  }
                }
                """;

            var (responseData, _) = await SuperAdminClient.PostGqlRequest(query);
            var root = responseData["data"]!["batchLoadEntities"]!.AsArray()
                .First(x => x!["thisId"]!.GetValue<string>() == "1");
            root!["childNodes"]!.AsArray().Count.ShouldBeGreaterThan(0);

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
                    childNodes {
                      thisId
                      firstChild { thisId }
                    }
                  }
                }
                """;

            var (responseData, _) = await SuperAdminClient.PostGqlRequest(query);
            responseData["data"]!["batchLoadEntitiesDisabled"]!.AsArray().Count.ShouldBe(5);
            var root = responseData["data"]!["batchLoadEntitiesDisabled"]!.AsArray()
                .First(x => x!["thisId"]!.GetValue<string>() == "1");
            var child = root!["childNodes"]!.AsArray()
                .First(x => x!["thisId"]!.GetValue<string>() == "1.1");
            child!["firstChild"]!["thisId"]!.GetValue<string>().ShouldBe("1.1.1");

            DB.StopProfiler();
        }

        [Fact]
        public async Task AutoBatchLoadOperationOverrideShouldEnableBatchLoadWhenGlobalDisabled()
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

        [Fact]
        public async Task AutoBatchLoadEmptyResultSetShouldNotThrow()
        {
            await SeedBatchLoadDataAsync();

            var query = """
                query {
                  batchLoadEntitiesFiltered(thisId: "nonexistent") {
                    thisId
                    childNodes { thisId firstChild { thisId } }
                  }
                }
                """;

            var (responseData, _) = await SuperAdminClient.PostGqlRequest(query);
            responseData["data"]!["batchLoadEntitiesFiltered"]!.AsArray().Count.ShouldBe(0);
        }

        [Fact]
        public async Task AutoBatchLoadEnabledMutationShouldBoundDatabaseQueries()
        {
            await SeedBatchLoadDataAsync();

            await DB.RestartProfiler();

            var query = """
                mutation {
                  batchLoadEntitiesEnabled {
                    thisId
                    firstChild { thisId }
                  }
                }
                """;

            var (responseData, _) = await SuperAdminClient.PostGqlRequest(query);
            responseData["data"]!["batchLoadEntitiesEnabled"]!.AsArray().Count.ShouldBe(5);

            DB.GetProfilerLogs().AsQueryable()
                .Count(x => x.ns != null && x.ns.Contains(ProfilerNamespace))
                .ShouldBe(2);

            DB.StopProfiler();
        }

        [Fact]
        public async Task AutoBatchLoadSubscriptionObservableShouldBoundDatabaseQueries()
        {
            await SeedBatchLoadDataAsync();

            await DB.RestartProfiler();

            var builder = ScopedService.GetRequiredService<IRequestExecutorBuilder>();
            var executor = await builder.BuildRequestExecutorAsync();

            await using var result = await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("""
                        subscription {
                          onBatchLoadEntities {
                            thisId
                            firstChild { thisId }
                          }
                        }
                        """)
                    .SetServices(ScopedService)
                    .Create());

            var stream = result.ExpectResponseStream();
            await foreach (var response in stream.ReadResultsAsync())
            {
                response.ExpectQueryResult().Data.ShouldNotBeNull();
                break;
            }

            DB.GetProfilerLogs().AsQueryable()
                .Count(x => x.ns != null && x.ns.Contains(ProfilerNamespace))
                .ShouldBe(2);

            DB.StopProfiler();
        }

        private async Task SeedBatchLoadDataAsync()
        {
            var uow = ScopedService.GetRequiredService<IUnitOfWork>();
            await uow.DeleteAsync<BatchLoadGraphQLEntity>(_ => true);

            uow.Attach(new BatchLoadGraphQLEntity("1"));
            uow.Attach(new BatchLoadGraphQLEntity("2"));
            uow.Attach(new BatchLoadGraphQLEntity("3"));
            uow.Attach(new BatchLoadGraphQLEntity("4"));
            uow.Attach(new BatchLoadGraphQLEntity("5"));
            uow.Attach(new BatchLoadGraphQLEntity("1.1", "1"));
            uow.Attach(new BatchLoadGraphQLEntity("1.2", "1"));
            uow.Attach(new BatchLoadGraphQLEntity("2.1", "2"));
            uow.Attach(new BatchLoadGraphQLEntity("1.1.1", "1.1"));
            await uow.SaveChanges();
        }
    }
}
