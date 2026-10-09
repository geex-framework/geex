using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading.Tasks;
using Geex;
using Geex.Gql.Attributes;
using Geex.Gql.AutoBatchLoad;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Entities.Interceptors;
using MongoDB.Entities.Utilities;
using Shouldly;

namespace MongoDB.Entities.Tests;

[TestClass]
[DoNotParallelize]
public class TestBatchLoadPolymorphism
{
    private static readonly string[] RootKeys = { "a1", "a2", "b1", "b2", "c1", "c2" };

    [TestMethod]
    public async Task renamed_interface_and_objects_load_without_a_graphql_storage_root()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Executor.Schema.TryGetType<IObjectType>(nameof(PolyRoot), out _).ShouldBeFalse();
        var data = await fixture.QueryAsync("{ roots { kind: __typename key common { key } } }");

        var roots = data.GetProperty("roots").EnumerateArray().ToArray();
        roots.Select(root => root.GetProperty("key").GetString()).ShouldBe(RootKeys);
        roots.Select(root => root.GetProperty("kind").GetString())
            .ShouldBe(new[] { "AlphaNode", "AlphaNode", "BetaNode", "BetaNode", "AdvancedNode", "AdvancedNode" });
        foreach (var root in roots)
            root.GetProperty("common")[0].GetProperty("key").GetString().ShouldBe(root.GetProperty("key").GetString() + "-common");
        QueryCount<PolyChild>().ShouldBe(3);
    }

    [TestMethod]
    public async Task same_named_navigation_uses_each_concrete_types_batch_rule()
    {
        await using var fixture = await Fixture.CreateAsync();
        var data = await fixture.QueryAsync("""
            { roots { key
                ... on AlphaNode { related { key } }
                ... on BetaNode { related { key } }
                ... on AdvancedNode { related { key } }
            } }
            """);
        foreach (var root in data.GetProperty("roots").EnumerateArray())
        {
            var key = root.GetProperty("key").GetString()!;
            root.GetProperty("related")[0].GetProperty("key").GetString().ShouldBe(key + (key.StartsWith('b') ? "-beta" : "-alpha"));
            root.GetProperty("related").GetArrayLength().ShouldBe(1);
        }
        QueryCount<PolyChild>().ShouldBe(3);
    }

    [TestMethod]
    public async Task fragment_on_base_object_does_not_load_other_graphql_object_types()
    {
        await using var fixture = await Fixture.CreateAsync();
        var data = await fixture.QueryAsync("{ roots { key ... on AlphaNode { related { key } } } }");

        foreach (var root in data.GetProperty("roots").EnumerateArray())
            root.TryGetProperty("related", out _).ShouldBe(root.GetProperty("key").GetString()!.StartsWith('a'));
        QueryCount<PolyChild>().ShouldBe(1);
    }

    [TestMethod]
    public async Task derived_related_entity_keeps_its_navigation_generic_type()
    {
        await using var fixture = await Fixture.CreateAsync();
        var data = await fixture.QueryAsync("{ roots { key ... on AlphaNode { derivedChildren { key } } } }");

        var roots = data.GetProperty("roots").EnumerateArray().Where(root => root.TryGetProperty("derivedChildren", out _)).ToArray();
        roots.Length.ShouldBe(2);
        roots[0].GetProperty("derivedChildren")[0].GetProperty("key").GetString().ShouldBe("a1-alpha");
        roots[1].GetProperty("derivedChildren")[0].GetProperty("key").GetString().ShouldBe("a2-alpha");
        QueryCount<PolyChild>().ShouldBe(1);
    }

    [TestMethod]
    public async Task manual_and_automatic_interface_paths_merge_without_duplicate_queries()
    {
        await using var fixture = await Fixture.CreateAsync();
        var data = await fixture.QueryAsync("{ roots(manual: true) { key common { key leaves { key } } } }");

        foreach (var root in data.GetProperty("roots").EnumerateArray())
            root.GetProperty("common")[0].GetProperty("leaves")[0].GetProperty("key").GetString()
                .ShouldBe(root.GetProperty("key").GetString() + "-common-leaf");
        QueryCount<PolyChild>().ShouldBe(3);
        QueryCount<PolyLeaf>().ShouldBe(3);
    }

    [TestMethod]
    public async Task manual_nested_paths_survive_a_scalar_only_selection()
    {
        await using var fixture = await Fixture.CreateAsync();
        var data = await fixture.QueryAsync("{ roots(manual: true) { key } }");

        data.GetProperty("roots").GetArrayLength().ShouldBe(6);
        QueryCount<PolyChild>().ShouldBe(3);
        QueryCount<PolyLeaf>().ShouldBe(3);
    }

    [TestMethod]
    public async Task interface_paging_collects_all_item_aliases_and_preserves_page_bounds()
    {
        await using var fixture = await Fixture.CreateAsync();
        var data = await fixture.QueryAsync("""
            { page(skip: 1, take: 3) {
                first: items { key common { key } }
                second: items { key ... on BetaNode { related { key } } }
                totalCount
            } }
            """);

        var page = data.GetProperty("page");
        page.GetProperty("totalCount").GetInt32().ShouldBe(6);
        page.GetProperty("first").EnumerateArray().Select(root => root.GetProperty("key").GetString())
            .ShouldBe(new[] { "a2", "b1", "b2" });
        page.GetProperty("second")[1].GetProperty("related")[0].GetProperty("key").GetString().ShouldBe("b1-beta");
        page.GetProperty("first")[2].GetProperty("common")[0].GetProperty("key").GetString().ShouldBe("b2-common");
        var commands = DB.DefaultDb.GetCollection<BsonDocument>("system.profile")
            .Find(new BsonDocument("ns", DB.DefaultDb.DatabaseNamespace.DatabaseName + "." + nameof(PolyChild)))
            .Project(new BsonDocument("command", 1)).ToList();
        Console.WriteLine(string.Join(Environment.NewLine, commands.Select(command => command.ToString())));
        // HC 13 的 offset 分页预读一行 c1, 因此有三个 Common 类型组和一个 Beta.Children 组.
        QueryCount<PolyChild>().ShouldBe(4);
    }

    [DataTestMethod]
    [DataRow(false, true, 1)]
    [DataRow(true, true, 0)]
    [DataRow(false, false, 0)]
    public async Task fragment_and_field_directives_control_batch_loading(bool skip, bool include, int expectedQueries)
    {
        await using var fixture = await Fixture.CreateAsync();
        var data = await fixture.QueryAsync("""
            query($skip: Boolean!, $include: Boolean!) {
                roots { key ... on AlphaNode @include(if: $include) { related @skip(if: $skip) { key } } }
            }
            """, new Dictionary<string, object?> { ["skip"] = skip, ["include"] = include });

        var first = data.GetProperty("roots")[0];
        first.TryGetProperty("related", out _).ShouldBe(expectedQueries == 1);
        QueryCount<PolyChild>().ShouldBe(expectedQueries);
    }

    [TestMethod]
    public async Task computed_property_on_a_polymorphic_object_loads_its_dependency()
    {
        await using var fixture = await Fixture.CreateAsync();
        var data = await fixture.QueryAsync("{ roots { key ... on AlphaNode { childCount } } }");

        data.GetProperty("roots")[0].GetProperty("childCount").GetInt32().ShouldBe(1);
        data.GetProperty("roots")[1].GetProperty("childCount").GetInt32().ShouldBe(1);
        QueryCount<PolyChild>().ShouldBe(1);
    }

    [TestMethod]
    public void instance_bound_sources_are_not_replaced_by_the_first_instances_source()
    {
        var first = new CustomRoot("a", new PolyChild { Key = "a-child", ParentKey = "a" });
        var second = new CustomRoot("b", new PolyChild { Key = "b-child", ParentKey = "b" });
        var config = new BatchLoadConfig();
        config.RegisterBatchLoad(typeof(CustomRoot).GetProperty(nameof(CustomRoot.Children))!, typeof(CustomRoot));

        new[] { first, second }.AsQueryable().BatchLoadLazyQueries(config);

        first.Children.Single().Key.ShouldBe("a-child");
        second.Children.Single().Key.ShouldBe("b-child");
        first.SourceCalls.ShouldBe(1);
        second.SourceCalls.ShouldBe(1);
    }

    [TestMethod]
    public async Task captured_batch_rules_are_evaluated_for_their_own_entity()
    {
        await using var fixture = await Fixture.CreateAsync();
        var first = fixture.Context.Attach(new CapturedRoot { Key = "a1", Flavor = "alpha" });
        var second = fixture.Context.Attach(new CapturedRoot { Key = "b1", Flavor = "beta" });
        var config = new BatchLoadConfig();
        config.RegisterBatchLoad(typeof(CapturedRoot).GetProperty(nameof(CapturedRoot.Children))!, typeof(CapturedRoot));

        new[] { first, second }.AsQueryable().BatchLoadLazyQueries(config);

        first.Children.Single().Key.ShouldBe("a1-alpha");
        second.Children.Single().Key.ShouldBe("b1-beta");
        QueryCount<PolyChild>().ShouldBe(2);
    }

    [TestMethod]
    public async Task default_sources_from_different_contexts_keep_their_own_filters()
    {
        await using var fixture = await Fixture.CreateAsync();
        using var otherContext = new DbContext();
        fixture.Context.DataFilters[typeof(PolyChild)] = new ExpressionDataFilter<PolyChild>(
            (Expression<Func<PolyChild, bool>>)(child => child.ParentKey == "a1"), null!);
        otherContext.DataFilters[typeof(PolyChild)] = new ExpressionDataFilter<PolyChild>(
            (Expression<Func<PolyChild, bool>>)(child => child.ParentKey == "a2"), null!);
        var first = fixture.Context.Attach(new AlphaRoot { Key = "a1" });
        var second = otherContext.Attach(new AlphaRoot { Key = "a2" });
        var config = new BatchLoadConfig();
        config.RegisterBatchLoad(typeof(AlphaRoot).GetProperty(nameof(AlphaRoot.Common))!, typeof(AlphaRoot));

        new[] { first, second }.AsQueryable().BatchLoadLazyQueries(config);

        first.Common.Single().Key.ShouldBe("a1-common");
        second.Common.Single().Key.ShouldBe("a2-common");
        QueryCount<PolyChild>().ShouldBe(2);
    }

    [DataTestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task count_skips_navigation_loading_and_preserves_local_entities(bool useLongCount, bool addLocal)
    {
        await using var fixture = await Fixture.CreateAsync();
        if (addLocal) fixture.Context.Attach(new AlphaRoot { Key = "local" });
        var query = fixture.Context.Query<IPolyRoot>().BatchLoad(root => root.Common);

        (useLongCount ? query.LongCount() : query.Count()).ShouldBe(addLocal ? 7 : 6);
        QueryCount<PolyChild>().ShouldBe(0);
        query.ToList().Count.ShouldBe(addLocal ? 7 : 6);
        QueryCount<PolyChild>().ShouldBe(3);
    }

    [TestMethod]
    public void custom_lazy_query_accepts_a_compiled_batch_delegate()
    {
        var root = new CustomRoot("a");
        Expression<Func<IQueryable<CustomRoot>, Expression<Func<PolyChild, bool>>>> batch =
            roots => child => roots.Select(parent => parent.Key).Contains(child.ParentKey);
        var lazy = new CustomLazyQuery(batch.Compile(), new[]
        {
            new PolyChild { Key = "a-child", ParentKey = "a" },
            new PolyChild { Key = "b-child", ParentKey = "b" }
        }.AsQueryable());
        root.LazyQueryCache[nameof(CustomRoot.Children)] = lazy;
        var config = new BatchLoadConfig();
        config.RegisterBatchLoad(typeof(CustomRoot).GetProperty(nameof(CustomRoot.Children))!, typeof(CustomRoot));

        new[] { root }.AsQueryable().BatchLoadLazyQueries(config);

        lazy.Source.Cast<PolyChild>().Select(child => child.Key).ShouldBe(new[] { "a-child" });
    }

    private static long QueryCount<T>() => DB.GetProfilerLogs().AsQueryable()
        .LongCount(entry => entry.ns == DB.DefaultDb.DatabaseNamespace.DatabaseName + "." + typeof(T).Name &&
                           (entry.op == "query" || entry.op == "command"));

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly ServiceProvider _provider;
        private readonly DbContext _context;
        public IRequestExecutor Executor { get; }
        public DbContext Context => _context;

        private Fixture(ServiceProvider provider, DbContext context, IRequestExecutor executor)
        {
            _provider = provider;
            _context = context;
            Executor = executor;
        }

        public static async Task<Fixture> CreateAsync()
        {
            if (!BsonClassMap.IsClassMapRegistered(typeof(PolyRoot)))
            {
                BsonClassMap.RegisterClassMap<PolyRoot>(map => { map.AutoMap(); map.SetIsRootClass(true); });
                BsonClassMap.RegisterClassMap<AlphaRoot>(map => map.AutoMap());
                BsonClassMap.RegisterClassMap<BetaRoot>(map => map.AutoMap());
                BsonClassMap.RegisterClassMap<AdvancedRoot>(map => map.AutoMap());
                BsonClassMap.RegisterClassMap<PolyChild>(map => { map.AutoMap(); map.SetIsRootClass(true); });
                BsonClassMap.RegisterClassMap<SpecialChild>(map => map.AutoMap());
            }
            DB.InterfaceCache[typeof(IPolyRoot)] = BsonClassMap.LookupClassMap(typeof(PolyRoot));
            DB.InterfaceCache[typeof(IPolyChild)] = BsonClassMap.LookupClassMap(typeof(PolyChild));
            await ReplaceAsync(nameof(PolyRoot), RootKeys.Select(key => Row(key, "", key.StartsWith('b')
                ? new[] { nameof(PolyRoot), nameof(BetaRoot) }
                : key.StartsWith('c') ? new[] { nameof(PolyRoot), nameof(AlphaRoot), nameof(AdvancedRoot) }
                : new[] { nameof(PolyRoot), nameof(AlphaRoot) })));
            var children = RootKeys.SelectMany(key => new[] { "common", "alpha", "beta" }.Select(flavor =>
            {
                var child = Row(key + "-" + flavor, key, flavor == "alpha"
                    ? new[] { nameof(PolyChild), nameof(SpecialChild) } : new[] { nameof(PolyChild) });
                child["Flavor"] = flavor;
                return child;
            })).ToArray();
            await ReplaceAsync(nameof(PolyChild), children);
            await ReplaceAsync(nameof(PolyLeaf), children.Select(child => Row(child["Key"].AsString + "-leaf", child["Key"].AsString)));
            var context = new DbContext();
            var services = new ServiceCollection().AddSingleton(context).AddSingleton(new GeexCoreModuleOptions());
            services.AddGraphQLServer()
                .AddQueryType<PolyQuery>(descriptor =>
                {
                    descriptor.Name(OperationTypeNames.Query);
                    descriptor.Field(query => query.GetPage(default!))
                        .UseOffsetPaging<InterfaceType<IPolyRoot>>(options: new() { IncludeTotalCount = true });
                })
                .AddType(new InterfaceType<IPolyRoot>(descriptor =>
                {
                    descriptor.Name("NodeContract");
                    descriptor.BindFieldsExplicitly();
                    descriptor.Field(root => root.Key);
                    descriptor.Field(root => root.Common);
                }))
                .AddType(new ObjectType<AlphaRoot>(descriptor =>
                {
                    descriptor.Name("AlphaNode");
                    descriptor.BindFieldsExplicitly();
                    descriptor.Implements<InterfaceType<IPolyRoot>>();
                    descriptor.Field(root => root.Key);
                    descriptor.Field(root => root.Common);
                    descriptor.Field(root => root.Children).Name("related");
                    descriptor.Field(root => root.DerivedChildren);
                    descriptor.Field(root => root.ChildCount);
                }))
                .AddType(new ObjectType<BetaRoot>(descriptor =>
                {
                    descriptor.Name("BetaNode");
                    descriptor.BindFieldsExplicitly();
                    descriptor.Implements<InterfaceType<IPolyRoot>>();
                    descriptor.Field(root => root.Key);
                    descriptor.Field(root => root.Common);
                    descriptor.Field(root => root.Children).Name("related");
                }))
                .AddType(new ObjectType<AdvancedRoot>(descriptor =>
                {
                    descriptor.Name("AdvancedNode");
                    descriptor.BindFieldsExplicitly();
                    descriptor.Implements<InterfaceType<IPolyRoot>>();
                    descriptor.Field(root => root.Key);
                    descriptor.Field(root => root.Common);
                    descriptor.Field(root => root.Children).Name("related");
                }))
                .AddType(new InterfaceType<IPolyChild>(descriptor =>
                {
                    descriptor.Name("ChildContract");
                    descriptor.BindFieldsExplicitly();
                    descriptor.Field(child => child.Key);
                    descriptor.Field(child => child.Leaves);
                }))
                .AddType(new ObjectType<PolyChild>(descriptor =>
                {
                    descriptor.Name("ChildNode");
                    descriptor.BindFieldsExplicitly();
                    descriptor.Implements<InterfaceType<IPolyChild>>();
                    descriptor.Field(child => child.Key);
                    descriptor.Field(child => child.Leaves);
                }))
                .AddType(new ObjectType<SpecialChild>(descriptor =>
                {
                    descriptor.Name("SpecialChildNode");
                    descriptor.BindFieldsExplicitly();
                    descriptor.Implements<InterfaceType<IPolyChild>>();
                    descriptor.Field(child => child.Key);
                    descriptor.Field(child => child.Leaves);
                }))
                .AddType(new ObjectType<PolyLeaf>(descriptor =>
                {
                    descriptor.BindFieldsExplicitly();
                    descriptor.Field(leaf => leaf.Key);
                }))
                .TryAddTypeInterceptor<AutoBatchLoadTypeInterceptor>();
            var provider = services.BuildServiceProvider();
            var executor = await provider.GetRequestExecutorAsync();
            await DB.RestartProfiler();
            return new Fixture(provider, context, executor);
        }

        public async Task<JsonElement> QueryAsync(string query, Dictionary<string, object?>? variables = null)
        {
            var request = QueryRequestBuilder.New().SetQuery(query);
            if (variables != null) request.SetVariableValues(variables);
            await using var result = await Executor.ExecuteAsync(request.Create());
            result.ExpectQueryResult().Errors.ShouldBeNull(result.ToJson());
            using var document = JsonDocument.Parse(result.ToJson());
            return document.RootElement.GetProperty("data").Clone();
        }

        private static BsonDocument Row(string key, string parentKey, string[]? types = null)
        {
            var row = new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "Key", key }, { "ParentKey", parentKey } };
            if (types != null) row["_t"] = new BsonArray(types);
            return row;
        }

        private static async Task ReplaceAsync(string collectionName, IEnumerable<BsonDocument> rows)
        {
            var collection = DB.DefaultDb.GetCollection<BsonDocument>(collectionName);
            await collection.DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);
            await collection.InsertManyAsync(rows);
        }

        public async ValueTask DisposeAsync()
        {
            DB.StopProfiler();
            _context.Dispose();
            await _provider.DisposeAsync();
        }
    }

    [GraphQLName(OperationTypeNames.Query)]
    public class PolyQuery
    {
        public IQueryable<IPolyRoot> GetRoots([Service] DbContext context, bool manual = false)
        {
            var query = context.Query<IPolyRoot>().OrderBy(root => root.Key);
            return manual ? query.BatchLoad(root => root.Common).ThenBatchLoad(child => child.Leaves) : query;
        }
        public IQueryable<IPolyRoot> GetPage([Service] DbContext context) => GetRoots(context);
    }

    public interface IPolyRoot : IEntityBase
    {
        string Key { get; }
        IQueryable<IPolyChild> Common { get; }
    }
    public class PolyRoot : EntityBase<PolyRoot>, IPolyRoot
    {
        protected PolyRoot() => ConfigLazyQuery(root => root.Common,
            child => child.ParentKey == Key && child.Flavor == "common",
            roots => child => roots.Select(root => root.Key).ToList().Contains(child.ParentKey) && child.Flavor == "common");
        public string Key { get; set; } = "";
        public string ParentKey { get; set; } = "";
        public IQueryable<IPolyChild> Common => LazyQuery(() => Common);
    }
    public class AlphaRoot : PolyRoot
    {
        public AlphaRoot()
        {
            ConfigLazyQuery<AlphaRoot, PolyChild>(root => root.Children, child => child.ParentKey == Key && child.Flavor == "alpha",
                roots => child => roots.Select(root => root.Key).ToList().Contains(child.ParentKey) && child.Flavor == "alpha");
            ConfigLazyQuery<AlphaRoot, SpecialChild>(root => root.DerivedChildren, child => child.ParentKey == Key,
                roots => child => roots.Select(root => root.Key).ToList().Contains(child.ParentKey));
        }
        public IQueryable<PolyChild> Children => LazyQuery(() => Children);
        public IQueryable<SpecialChild> DerivedChildren => LazyQuery(() => DerivedChildren);
        [AutoBatchLoadDependsOn(nameof(Children))]
        public int ChildCount => Children.Count();
    }
    public class BetaRoot : PolyRoot
    {
        public BetaRoot() => ConfigLazyQuery<BetaRoot, PolyChild>(root => root.Children,
            child => child.ParentKey == Key && child.Flavor == "beta",
            roots => child => roots.Select(root => root.Key).ToList().Contains(child.ParentKey) && child.Flavor == "beta");
        public IQueryable<PolyChild> Children => LazyQuery(() => Children);
    }
    public class AdvancedRoot : AlphaRoot { }
    public interface IPolyChild : IEntityBase
    {
        string Key { get; }
        string ParentKey { get; }
        string Flavor { get; }
        IQueryable<PolyLeaf> Leaves { get; }
    }
    public class PolyChild : EntityBase<PolyChild>, IPolyChild
    {
        public PolyChild() => ConfigLazyQuery(child => child.Leaves, leaf => leaf.ParentKey == Key,
            children => leaf => children.Select(child => child.Key).ToList().Contains(leaf.ParentKey));
        public string Key { get; set; } = "";
        public string ParentKey { get; set; } = "";
        public string Flavor { get; set; } = "";
        public IQueryable<PolyLeaf> Leaves => LazyQuery(() => Leaves);
    }
    public class SpecialChild : PolyChild { }
    public class PolyLeaf : EntityBase<PolyLeaf>
    {
        public string Key { get; set; } = "";
        public string ParentKey { get; set; } = "";
    }
    public class CustomRoot : EntityBase<CustomRoot>
    {
        private readonly PolyChild[] _children = Array.Empty<PolyChild>();
        public int SourceCalls { get; private set; }
        public CustomRoot() => ConfigLazyQuery(root => root.Children, child => child.ParentKey == Key,
            roots => child => roots.Select(root => root.Key).ToList().Contains(child.ParentKey),
            () => { SourceCalls++; return _children.AsQueryable(); });
        public CustomRoot(string key, params PolyChild[] children) : this() { Key = key; _children = children; }
        public string Key { get; set; } = "";
        public IQueryable<PolyChild> Children => LazyQuery(() => Children);
    }

    public class CapturedRoot : EntityBase<CapturedRoot>
    {
        public CapturedRoot() => ConfigLazyQuery(root => root.Children,
            child => child.ParentKey == Key && child.Flavor == Flavor,
            roots => child => roots.Select(root => root.Key).ToList().Contains(child.ParentKey) && child.Flavor == Flavor);
        public string Key { get; set; } = "";
        public string Flavor { get; set; } = "";
        public IQueryable<PolyChild> Children => LazyQuery(() => Children);
    }

    private sealed class CustomLazyQuery(Delegate batch, IQueryable source) : ILazyQuery
    {
        public Delegate BatchQuery => batch;
        public Func<IQueryable> DefaultSourceProvider => () => source;
        public Delegate LazyQuery => (Func<PolyChild, bool>)(_ => true);
        public object Value => Source;
        public IQueryable Source { get; set; } = source;
    }
}
