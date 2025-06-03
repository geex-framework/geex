using Geex.Extensions.BackgroundJob;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using System;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Entities;

namespace Geex.Tests.FeatureTests
{
    [Collection(nameof(TestsCollection))]
    public class BackgroundJobApiTests : TestsBase
    {
        public BackgroundJobApiTests(TestApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task QueryJobStateShouldWork()
        {
            // Arrange
            var client = this.SuperAdminClient;
            var query = """
                        query {
                            jobState(skip: 0, take: 10) {
                                items { id jobName lastExecutionTime }
                                pageInfo { hasPreviousPage hasNextPage }
                                totalCount
                            }
                        }
                        """;

            await Task.Delay(1500);

            // Act
            var (responseData, responseString) = await client.PostGqlRequest(query);

            // Assert
            int totalCount = responseData["data"]["jobState"]["totalCount"].GetValue<int>();
            totalCount.ShouldBeGreaterThan(0);

            var items = responseData["data"]["jobState"]["items"].AsArray();
            items.Any(item => (string)item["jobName"] == nameof(TestStatefulCronJob)).ShouldBeTrue();
        }

        [Fact]
        public async Task FilterJobStateByJobNameShouldWork()
        {
            // Arrange
            var client = this.SuperAdminClient;

            var query = """
                        query($jobName: String!) {
                            jobState(skip: 0, take: 10, filter: { jobName: { eq: $jobName } }) {
                                items { id jobName }
                                totalCount
                            }
                        }
                        """;
            await Task.Delay(1500);

            // Act
            var (responseData, responseString) =
                await client.PostGqlRequest(query, new { jobName = nameof(TestStatefulCronJob) });

            // Assert
            var items = responseData["data"]["jobState"]["items"].AsArray();
            items.Count.ShouldBeGreaterThan(0);

            foreach (var item in items)
            {
                ((string)item["jobName"]).ShouldBe(nameof(TestStatefulCronJob));
            }
        }

        [Fact]
        public async Task QueryJobExecutionHistoryShouldWork()
        {
            // Arrange
            var client = this.SuperAdminClient;

            var query = """
                        query($jobName: String!) {
                            jobState(skip: 0, take: 10, filter: { jobName: { eq: $jobName } }) {
                                items { id jobName executionHistories {
                                        items { jobName  isSuccess }
                                    }
                                }
                                totalCount
                            }
                        }
                        """;

            await Task.Delay(1500);

            // Act
            var (responseData, responseString) =
                await client.PostGqlRequest(query, new { jobName = nameof(TestStatefulCronJob) });

            // Assert
            var items = responseData["data"]["jobState"]["items"][0]["executionHistories"]["items"].AsArray();
            items.Count.ShouldBeGreaterThan(0);

            // Verify all items belong to our test job
            foreach (var item in items)
            {
                ((string)item["jobName"]).ShouldBe(nameof(TestStatefulCronJob));
                ((bool)item["isSuccess"]).ShouldBe(true);
            }
        }
    }
}
