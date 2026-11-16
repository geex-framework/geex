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

        using (var singleContext = new DbContext(ScopedService, entityTrackingEnabled: tracking))
        {
            var children = singleContext.Query<QueryableContainsTestEntity>()
                .Where(x => x.Group == group && x.ParentId != null)
                .BatchLoad(x => x.Parent)
                .ToList();
            Assert.Equal(4, children.Count);
            Assert.Equal(parentA.Id, children.Single(x => x.Id == childA.Id).Parent.Value.Id);
            Assert.Equal(parentA.Id, children.Single(x => x.Id == secondChildA.Id).Parent.Value.Id);
            Assert.Equal(parentB.Id, children.Single(x => x.Id == childB.Id).Parent.Value.Id);
            Assert.Null(children.Single(x => x.Id == orphan.Id).Parent.Value);
        }

        using var multipleContext = new DbContext(ScopedService, entityTrackingEnabled: tracking);
        var parents = multipleContext.Query<QueryableContainsTestEntity>()
            .Where(x => x.Group == group && x.ParentId == null)
            .BatchLoad(x => x.Children)
            .ThenBatchLoad(x => x.Parent)
            .ToList();
        Assert.Equal(3, parents.Count);
        Assert.Equal(new[] { childA.Id, secondChildA.Id }.OrderBy(x => x),
            parents.Single(x => x.Id == parentA.Id).Children.Select(x => x.Id).OrderBy(x => x));
        Assert.Equal(childB.Id, Assert.Single(parents.Single(x => x.Id == parentB.Id).Children).Id);
        Assert.Empty(parents.Single(x => x.Id == emptyParent.Id).Children);
        foreach (var parent in parents)
            Assert.All(parent.Children, child => Assert.Equal(parent.Id, child.Parent.Value.Id));
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
        var rows = context.Query<QueryableContainsTestEntity>()
            .Where(x => x.Group == group && selected.Select(item => item.Label).Contains(x.Label))
            .ToList();
        Assert.Equal(matchingId, Assert.Single(rows).Id);
        Assert.Equal(otherId, Assert.Single(context.Query<QueryableContainsTestEntity>()
            .Where(x => x.Group == group && !selected.Select(item => item.Label).Contains(x.Label))).Id);

        var empty = Array.Empty<string>().AsQueryable();
        Assert.Empty(context.Query<QueryableContainsTestEntity>()
            .Where(x => x.Group == group && empty.Contains(x.Label)));
    }

    [Fact]
    public void QueryableMembershipWithCustomComparerRemainsUnsupported()
    {
        var labels = new[] { "value" }.AsQueryable();
        using var context = new DbContext(ScopedService, entityTrackingEnabled: false);
        var error = Assert.Throws<InvalidQueryException>(() => context.Query<QueryableContainsTestEntity>()
            .Where(x => labels.Contains(x.Label, StringComparer.OrdinalIgnoreCase)).ToList());
        Assert.Equal("Unsupported Call Expression for method Contains", error.Message);
    }
}
