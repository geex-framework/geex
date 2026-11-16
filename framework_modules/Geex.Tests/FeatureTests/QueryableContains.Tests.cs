using System.Text.Json;

using MongoDB.Bson;
using MongoDB.Entities;
using MongoDB.Entities.InnerQuery;

namespace Geex.Tests.FeatureTests;

public class QueryableContainsTestEntity : EntityBase<QueryableContainsTestEntity>
{
    public QueryableContainsTestEntity()
    {
        ConfigLazyQuery(x => x.Parent, parent => parent.Id == ParentId,
            entities => parent => entities.Select(x => x.ParentId).Contains(parent.Id));
        ConfigLazyQuery(x => x.Children, child => child.ParentId == Id,
            entities => child => entities.Select(x => x.Id).Contains(child.ParentId));
    }

    public string Group { get; set; } = "";
    public string Label { get; set; } = "";
    [ObjectId]
    public string? ParentId { get; set; }
    public Lazy<QueryableContainsTestEntity> Parent => LazyQuery(() => Parent);
    public IQueryable<QueryableContainsTestEntity> Children => LazyQuery(() => Children);
}

[Collection(nameof(TestsCollection))]
public class QueryableContainsTests(TestApplicationFactory factory) : TestsBase(factory)
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ProjectedQueryableIdsBatchLoadSingleAndMultipleNavigations(bool tracking)
    {
        var group = Guid.NewGuid().ToString("N");
        QueryableContainsTestEntity parentA, parentB, emptyParent, childA, secondChildA, childB, orphan;
        using (var seed = new DbContext(ScopedService))
        {
            parentA = seed.Attach(new QueryableContainsTestEntity { Group = group, Label = "parent-a" });
            parentB = seed.Attach(new QueryableContainsTestEntity { Group = group, Label = "parent-b" });
            emptyParent = seed.Attach(new QueryableContainsTestEntity { Group = group, Label = "empty" });
            childA = seed.Attach(new QueryableContainsTestEntity { Group = group, ParentId = parentA.Id });
            secondChildA = seed.Attach(new QueryableContainsTestEntity { Group = group, ParentId = parentA.Id });
            childB = seed.Attach(new QueryableContainsTestEntity { Group = group, ParentId = parentB.Id });
            orphan = seed.Attach(new QueryableContainsTestEntity
            {
                Group = group,
                ParentId = ObjectId.GenerateNewId().ToString()
            });
            await seed.SaveChanges();
        }

        object singleObservation;
        using (var singleContext = new DbContext(ScopedService, entityTrackingEnabled: tracking))
        {
            var singleQuery = singleContext.Query<QueryableContainsTestEntity>()
                .Where(x => x.Group == group && x.ParentId != null)
                .BatchLoad(x => x.Parent);
            var children = singleQuery.ToList();
            Assert.Equal(4, children.Count);
            Assert.Equal(parentA.Id, children.Single(x => x.Id == childA.Id).Parent.Value.Id);
            Assert.Equal(parentA.Id, children.Single(x => x.Id == secondChildA.Id).Parent.Value.Id);
            Assert.Equal(parentB.Id, children.Single(x => x.Id == childB.Id).Parent.Value.Id);
            Assert.Null(children.Single(x => x.Id == orphan.Id).Parent.Value);
            singleObservation = new
            {
                expression = singleQuery.Expression.ToString(),
                rows = children.Select(x => new { x.Id, parentId = x.Parent.Value?.Id }).ToArray()
            };
        }

        using var multipleContext = new DbContext(ScopedService, entityTrackingEnabled: tracking);
        var multipleQuery = multipleContext.Query<QueryableContainsTestEntity>()
            .Where(x => x.Group == group && x.ParentId == null)
            .BatchLoad(x => x.Children)
            .ThenBatchLoad(x => x.Parent);
        var parents = multipleQuery.ToList();
        Assert.Equal(3, parents.Count);
        Assert.Equal(new[] { childA.Id, secondChildA.Id }.OrderBy(x => x),
            parents.Single(x => x.Id == parentA.Id).Children.Select(x => x.Id).OrderBy(x => x));
        Assert.Equal(childB.Id, Assert.Single(parents.Single(x => x.Id == parentB.Id).Children).Id);
        Assert.Empty(parents.Single(x => x.Id == emptyParent.Id).Children);
        foreach (var parent in parents)
            Assert.All(parent.Children, child => Assert.Equal(parent.Id, child.Parent.Value.Id));
        await Evidence($"{nameof(ProjectedQueryableIdsBatchLoadSingleAndMultipleNavigations)}-tracking-{tracking}", new
        {
            group,
            tracking,
            single = singleObservation,
            multiple = new
            {
                expression = multipleQuery.Expression.ToString(),
                rows = parents.Select(x => new
                {
                    x.Id,
                    children = x.Children.Select(child => new { child.Id, parentId = child.Parent.Value.Id }).ToArray()
                }).ToArray()
            }
        });
    }

    [Fact]
    public async Task ProjectedQueryableMembershipPreservesStringsDuplicatesAndEmptySets()
    {
        var group = Guid.NewGuid().ToString("N");
        var matchingLabel = ObjectId.GenerateNewId().ToString();
        string matchingId, otherId;
        using (var seed = new DbContext(ScopedService))
        {
            matchingId = seed.Attach(new QueryableContainsTestEntity { Group = group, Label = matchingLabel }).Id;
            otherId = seed.Attach(new QueryableContainsTestEntity { Group = group, Label = "other" }).Id;
            await seed.SaveChanges();
        }

        var selected = new[] { new { Label = matchingLabel }, new { Label = matchingLabel } }.AsQueryable();
        using var context = new DbContext(ScopedService, entityTrackingEnabled: false);
        var matchingQuery = context.Query<QueryableContainsTestEntity>()
            .Where(x => x.Group == group && selected.Select(item => item.Label).Contains(x.Label));
        var rows = matchingQuery.ToList();
        Assert.Equal(matchingId, Assert.Single(rows).Id);
        var otherQuery = context.Query<QueryableContainsTestEntity>()
            .Where(x => x.Group == group && !selected.Select(item => item.Label).Contains(x.Label));
        var otherRows = otherQuery.ToList();
        Assert.Equal(otherId, Assert.Single(otherRows).Id);

        var empty = Array.Empty<string>().AsQueryable();
        var emptyQuery = context.Query<QueryableContainsTestEntity>()
            .Where(x => x.Group == group && empty.Contains(x.Label));
        var emptyRows = emptyQuery.ToList();
        Assert.Empty(emptyRows);
        await Evidence(nameof(ProjectedQueryableMembershipPreservesStringsDuplicatesAndEmptySets), new
        {
            group,
            selected = selected.ToArray(),
            matching = new { expression = matchingQuery.Expression.ToString(), rows = rows.Select(x => new { x.Id, x.Label }).ToArray() },
            other = new { expression = otherQuery.Expression.ToString(), rows = otherRows.Select(x => new { x.Id, x.Label }).ToArray() },
            empty = new { expression = emptyQuery.Expression.ToString(), ids = emptyRows.Select(x => x.Id).ToArray() }
        });
    }

    [Fact]
    public async Task QueryableMembershipWithCustomComparerRemainsUnsupported()
    {
        var labels = new[] { "value" }.AsQueryable();
        using var context = new DbContext(ScopedService, entityTrackingEnabled: false);
        var query = context.Query<QueryableContainsTestEntity>()
            .Where(x => labels.Contains(x.Label, StringComparer.OrdinalIgnoreCase));
        var error = Assert.Throws<InvalidQueryException>(() => query.ToList());
        Assert.Equal("Unsupported Call Expression for method Contains", error.Message);
        await Evidence(nameof(QueryableMembershipWithCustomComparerRemainsUnsupported), new
        {
            expression = query.Expression.ToString(),
            labels = labels.ToArray(),
            error = new { type = error.GetType().FullName, error.Message }
        });
    }

    private static async Task Evidence(string testCase, object observation)
    {
        var root = Environment.GetEnvironmentVariable("PP_BACKEND_EVIDENCE");
        if (string.IsNullOrWhiteSpace(root)) return;
        var directory = Path.Combine(root, "cases");
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, $"queryable-contains-{testCase}.json"),
            JsonSerializer.Serialize(new { testCase, observations = new[] { observation } }));
    }
}
