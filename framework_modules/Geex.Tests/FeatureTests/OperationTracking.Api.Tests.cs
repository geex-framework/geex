using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Geex.Tests.SchemaTests.TestEntities;
using MongoDB.Driver;
using MongoDB.Entities;
using Shouldly;

namespace Geex.Tests.FeatureTests;

[Collection(nameof(TestsCollection))]
public sealed class OperationTrackingApiTests(TestApplicationFactory factory) : TestsBase(factory)
{
    [Theory]
    [InlineData("query Named { unitOfWorkTracking queryProviderTracking }")]
    [InlineData("query($include: Boolean! = true) { unitOfWorkTracking @include(if: $include) queryProviderTracking }")]
    [InlineData("{ unitOfWorkTracking queryProviderTracking }")]
    [InlineData("\n# tracking probe\nquery { unitOfWorkTracking queryProviderTracking }")]
    public async Task QueriesDisableTrackingBeforeCreatingProviders(string query)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var result = await Execute(query);
            result["data"]!["unitOfWorkTracking"]!.GetValue<bool>().ShouldBeFalse();
            result["data"]!["queryProviderTracking"]!.GetValue<bool>().ShouldBeFalse();
        }
    }

    [Fact]
    public async Task SelectedOperationControlsTrackingAndMutationPersists()
    {
        const string document = """
            query Read { unitOfWorkTracking queryProviderTracking }
            mutation Write($id: String!) { saveBatchLoadTrackingProbe(thisId: $id) }
            """;
        var marker = "tracking-" + Guid.NewGuid().ToString("N");
        var collection = DB.Collection<BatchLoadTestEntity>();
        try
        {
            for (var attempt = 0; attempt < 2; attempt++)
            {
                var read = await Execute(document, "Read");
                read["data"]!["unitOfWorkTracking"]!.GetValue<bool>().ShouldBeFalse();
                read["data"]!["queryProviderTracking"]!.GetValue<bool>().ShouldBeFalse();

                var write = await Execute(document, "Write", new { id = marker });
                write["data"]!["saveBatchLoadTrackingProbe"]!.GetValue<bool>().ShouldBeTrue();
                (await collection.CountDocumentsAsync(entity => entity.ThisId == marker)).ShouldBe(attempt + 1);
            }

            using var invalid = await SuperAdminClient.PostAsJsonAsync("/graphql", new { query = document, operationName = "Missing" });
            var invalidResult = JsonNode.Parse(await invalid.Content.ReadAsStringAsync())!;
            invalidResult["errors"].ShouldNotBeNull();
        }
        finally
        {
            await collection.DeleteManyAsync(entity => entity.ThisId == marker);
        }
    }

    private async Task<JsonNode> Execute(string query, string? operationName = null, object? variables = null)
    {
        using var response = await SuperAdminClient.PostAsJsonAsync("/graphql", new { query, operationName, variables });
        response.EnsureSuccessStatusCode();
        var result = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        result["errors"].ShouldBeNull(result.ToJsonString());
        return result;
    }
}
