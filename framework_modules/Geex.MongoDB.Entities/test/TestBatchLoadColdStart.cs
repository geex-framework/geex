using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Geex;
using Geex.Gql;
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
using MongoDB.Entities.Utilities;
using Shouldly;

namespace MongoDB.Entities.Tests;

[TestClass]
[DoNotParallelize]
public class TestBatchLoadColdStart
{
    [TestMethod]
    public void auto_batch_load_configuration_preserves_unset_and_explicit_values()
    {
        var features = new GeexFeatures(new ExtensionData());
        features.AutoBatchLoad.ShouldBeNull();

        features.AutoBatchLoad = new AutoBatchLoadFeatureConfig(false);
        (features.AutoBatchLoad?.IsEnabled).ShouldBe(false);
        features.AutoBatchLoad = new AutoBatchLoadFeatureConfig(true);
        (features.AutoBatchLoad?.IsEnabled).ShouldBe(true);
        features.AutoBatchLoad = null;
        features.AutoBatchLoad.ShouldBeNull();
    }

    [TestMethod]
    public async Task global_auto_batch_load_can_be_disabled()
    {
        (await GetAutoBatchLoadSettingAsync(false, null)).ShouldBeFalse();
    }

    [TestMethod]
    public async Task explicit_auto_batch_load_true_overrides_global_false()
    {
        (await GetAutoBatchLoadSettingAsync(false, true)).ShouldBeTrue();
    }

    [TestMethod]
    public async Task explicit_auto_batch_load_false_overrides_global_true()
    {
        (await GetAutoBatchLoadSettingAsync(true, false)).ShouldBeFalse();
    }

    [TestMethod]
    public async Task first_manual_batch_load_initializes_nested_types_before_materialization()
    {
        await SeedAsync<ManualRoot, ManualChild, ManualLeaf>();
        using var context = new DbContext();
        ManualRoot.ConstructorCalls.ShouldBe(0);
        ManualChild.ConstructorCalls.ShouldBe(0);
        var query = context.Query<ManualRoot>().BatchLoad(x => x.Children).ThenBatchLoad(x => x.Leaves);
        ManualRoot.ConstructorCalls.ShouldBe(1);
        ManualChild.ConstructorCalls.ShouldBe(1);

        await DB.RestartProfiler();
        try
        {
            var roots = query.OrderBy(x => x.Key).ToList();

            roots.Select(x => x.Key).ShouldBe(new[] { "a", "b" });
            foreach (var root in roots)
            {
                var child = root.Children.Single();
                child.Key.ShouldBe(root.Key + "1");
                child.Leaves.Single().Key.ShouldBe(root.Key + "11");
            }
            roots.SelectMany(x => x.Children).Select(x => x.Key).OrderBy(x => x).ShouldBe(new[] { "a1", "b1" });
            roots.SelectMany(x => x.Children).SelectMany(x => x.Leaves).Select(x => x.Key).OrderBy(x => x)
                .ShouldBe(new[] { "a11", "b11" });
            QueryCount<ManualRoot>().ShouldBe(1);
            QueryCount<ManualChild>().ShouldBe(1);
            QueryCount<ManualLeaf>().ShouldBe(1);
        }
        finally
        {
            DB.StopProfiler();
        }
    }

    [TestMethod]
    public async Task first_automatic_batch_load_resolves_interface_and_nested_navigation()
    {
        await SeedAsync<AutomaticRoot, AutomaticChild, AutomaticLeaf>();
        BsonClassMap.LookupClassMap(typeof(AutomaticRoot));
        DB.InterfaceCache[typeof(IAutomaticRoot)] = BsonClassMap.LookupClassMap(typeof(AutomaticRoot));
        using var context = new DbContext();
        var services = new ServiceCollection().AddSingleton(new GeexCoreModuleOptions());
        services.AddGraphQLServer()
            .AddQueryType<AutomaticQuery>()
            .AddType(new ObjectType<AutomaticRoot>(descriptor =>
            {
                descriptor.Name(nameof(AutomaticRoot));
                descriptor.BindFieldsExplicitly();
                descriptor.Field(x => x.Key);
                descriptor.Field(x => x.Children);
            }))
            .AddType(new ObjectType<AutomaticChild>(descriptor =>
            {
                descriptor.BindFieldsExplicitly();
                descriptor.Field(x => x.Key);
                descriptor.Field(x => x.Leaves);
            }))
            .AddType(new ObjectType<AutomaticLeaf>(descriptor =>
            {
                descriptor.BindFieldsExplicitly();
                descriptor.Field(x => x.Key);
            }))
            .BindRuntimeType<IAutomaticRoot, ObjectType<AutomaticRoot>>()
            .TryAddTypeInterceptor<AutoBatchLoadTypeInterceptor>();
        services.AddSingleton(context);
        await using var provider = services.BuildServiceProvider();
        var executor = await provider.GetRequestExecutorAsync();
        (executor.Schema.QueryType.Fields["roots"].GeexFeatures.AutoBatchLoad?.IsEnabled).ShouldBe(true);
        AutomaticRoot.ConstructorCalls.ShouldBe(0);
        AutomaticChild.ConstructorCalls.ShouldBe(0);

        await DB.RestartProfiler();
        try
        {
            await using var result = await executor.ExecuteAsync("{ roots { key children { key leaves { key } } } }");
            using var json = JsonDocument.Parse(result.ToJson());

            result.ExpectQueryResult().Errors.ShouldBeNull(result.ToJson());
            var roots = json.RootElement.GetProperty("data").GetProperty("roots").EnumerateArray().ToArray();
            roots.Length.ShouldBe(2);
            foreach (var root in roots)
            {
                var key = root.GetProperty("key").GetString();
                var child = root.GetProperty("children").EnumerateArray().Single();
                child.GetProperty("key").GetString().ShouldBe(key + "1");
                child.GetProperty("leaves").EnumerateArray().Single().GetProperty("key").GetString().ShouldBe(key + "11");
            }
            QueryCount<AutomaticRoot>().ShouldBe(1);
            QueryCount<AutomaticChild>().ShouldBe(1);
            QueryCount<AutomaticLeaf>().ShouldBe(1);
        }
        finally
        {
            DB.StopProfiler();
        }
    }

    [TestMethod]
    public async Task first_automatic_batch_load_initializes_computed_field_dependencies()
    {
        await SeedAsync<ComputedRoot, ComputedChild, ComputedLeaf>();
        using var context = new DbContext();
        var services = new ServiceCollection().AddSingleton(new GeexCoreModuleOptions());
        services.AddGraphQLServer()
            .AddQueryType<ComputedQuery>()
            .AddType(new ObjectType<ComputedRoot>(descriptor =>
            {
                descriptor.BindFieldsExplicitly();
                descriptor.Field(x => x.Key);
                descriptor.Field(x => x.ChildCount);
            }))
            .TryAddTypeInterceptor<AutoBatchLoadTypeInterceptor>();
        services.AddSingleton(context);
        await using var provider = services.BuildServiceProvider();
        var executor = await provider.GetRequestExecutorAsync();
        (executor.Schema.QueryType.Fields["roots"].GeexFeatures.AutoBatchLoad?.IsEnabled).ShouldBe(true);
        ComputedRoot.ConstructorCalls.ShouldBe(0);

        await DB.RestartProfiler();
        try
        {
            await using var result = await executor.ExecuteAsync("{ roots { key childCount } }");
            using var json = JsonDocument.Parse(result.ToJson());

            result.ExpectQueryResult().Errors.ShouldBeNull(result.ToJson());
            json.RootElement.GetProperty("data").GetProperty("roots").EnumerateArray()
                .Select(x => x.GetProperty("childCount").GetInt32()).ShouldBe(new[] { 1, 1 });
            QueryCount<ComputedRoot>().ShouldBe(1);
            QueryCount<ComputedChild>().ShouldBe(1);
        }
        finally
        {
            DB.StopProfiler();
        }
    }

    private static async Task<bool> GetAutoBatchLoadSettingAsync(bool globalDefault, bool? fieldOverride)
    {
        var services = new ServiceCollection().AddSingleton(new GeexCoreModuleOptions { AutoBatchLoad = globalDefault });
        services.AddGraphQLServer()
            .AddQueryType<ConfigurationQuery>(descriptor =>
            {
                if (fieldOverride is { } enabled)
                {
                    descriptor.Field(x => x.GetRoots()).UseAutoBatchLoad(enabled);
                }
            })
            .AddType(new ObjectType<ConfigurationEntity>(descriptor =>
            {
                descriptor.BindFieldsExplicitly();
                descriptor.Field(x => x.Key);
            }))
            .TryAddTypeInterceptor<AutoBatchLoadTypeInterceptor>();
        await using var provider = services.BuildServiceProvider();
        var executor = await provider.GetRequestExecutorAsync();
        return executor.Schema.QueryType.Fields["roots"].GeexFeatures.AutoBatchLoad!.Value.IsEnabled;
    }

    private static async Task SeedAsync<TRoot, TChild, TLeaf>()
    {
        await InsertRawAsync(typeof(TRoot).Name, ("a", ""), ("b", ""));
        await InsertRawAsync(typeof(TChild).Name, ("a1", "a"), ("b1", "b"));
        await InsertRawAsync(typeof(TLeaf).Name, ("a11", "a1"), ("b11", "b1"));
    }

    private static async Task InsertRawAsync(string collectionName, params (string Key, string ParentKey)[] rows)
    {
        var collection = DB.DefaultDb.GetCollection<BsonDocument>(collectionName);
        await collection.DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);
        await collection.InsertManyAsync(rows.Select(row => new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() }, { "Key", row.Key }, { "ParentKey", row.ParentKey }
        }));
    }

    private static long QueryCount<T>() => DB.GetProfilerLogs().AsQueryable()
        .LongCount(x => x.ns == DB.DefaultDb.DatabaseNamespace.DatabaseName + "." + typeof(T).Name &&
            (x.op == "query" || x.op == "command"));

    [GraphQLName(OperationTypeNames.Query)]
    public class ConfigurationQuery
    {
        public IQueryable<ConfigurationEntity> GetRoots() => Array.Empty<ConfigurationEntity>().AsQueryable();
    }
    public class ConfigurationEntity : EntityBase<ConfigurationEntity>
    {
        public string Key { get; set; } = "";
    }

    public class ManualRoot : EntityBase<ManualRoot>
    {
        public static int ConstructorCalls;
        public ManualRoot()
        {
            ConstructorCalls++;
            ConfigLazyQuery(x => x.Children, x => x.ParentKey == Key, roots => x => roots.Select(r => r.Key).ToList().Contains(x.ParentKey));
        }
        public string Key { get; set; } = "";
        public IQueryable<ManualChild> Children => LazyQuery(() => Children);
    }
    public class ManualChild : EntityBase<ManualChild>
    {
        public static int ConstructorCalls;
        public ManualChild()
        {
            ConstructorCalls++;
            ConfigLazyQuery(x => x.Leaves, x => x.ParentKey == Key, children => x => children.Select(c => c.Key).ToList().Contains(x.ParentKey));
        }
        public string Key { get; set; } = "";
        public string ParentKey { get; set; } = "";
        public IQueryable<ManualLeaf> Leaves => LazyQuery(() => Leaves);
    }
    public class ManualLeaf : EntityBase<ManualLeaf>
    {
        public string Key { get; set; } = "";
        public string ParentKey { get; set; } = "";
    }

    public interface IAutomaticRoot : IEntityBase
    {
        string Key { get; }
        IQueryable<AutomaticChild> Children { get; }
    }
    public class AutomaticRoot : EntityBase<AutomaticRoot>, IAutomaticRoot
    {
        public static int ConstructorCalls;
        public AutomaticRoot()
        {
            ConstructorCalls++;
            ConfigLazyQuery(x => x.Children, x => x.ParentKey == Key, roots => x => roots.Select(r => r.Key).ToList().Contains(x.ParentKey));
        }
        public string Key { get; set; } = "";
        public IQueryable<AutomaticChild> Children => LazyQuery(() => Children);
    }
    public class AutomaticChild : EntityBase<AutomaticChild>
    {
        public static int ConstructorCalls;
        public AutomaticChild()
        {
            ConstructorCalls++;
            ConfigLazyQuery(x => x.Leaves, x => x.ParentKey == Key, children => x => children.Select(c => c.Key).ToList().Contains(x.ParentKey));
        }
        public string Key { get; set; } = "";
        public string ParentKey { get; set; } = "";
        public IQueryable<AutomaticLeaf> Leaves => LazyQuery(() => Leaves);
    }
    public class AutomaticLeaf : EntityBase<AutomaticLeaf>
    {
        public string Key { get; set; } = "";
        public string ParentKey { get; set; } = "";
    }
    [GraphQLName(OperationTypeNames.Query)]
    public class AutomaticQuery
    {
        public IQueryable<IAutomaticRoot> GetRoots([Service] DbContext context) => context.Query<IAutomaticRoot>();
    }

    public class ComputedRoot : EntityBase<ComputedRoot>
    {
        public static int ConstructorCalls;
        public ComputedRoot()
        {
            ConstructorCalls++;
            ConfigLazyQuery(x => x.Children, x => x.ParentKey == Key, roots => x => roots.Select(r => r.Key).ToList().Contains(x.ParentKey));
        }
        public string Key { get; set; } = "";
        public IQueryable<ComputedChild> Children => LazyQuery(() => Children);
        [AutoBatchLoadDependsOn(nameof(Children))]
        public int ChildCount => Children.Count();
    }
    public class ComputedChild : EntityBase<ComputedChild>
    {
        public string Key { get; set; } = "";
        public string ParentKey { get; set; } = "";
    }
    public class ComputedLeaf : EntityBase<ComputedLeaf>
    {
        public string Key { get; set; } = "";
        public string ParentKey { get; set; } = "";
    }
    [GraphQLName(OperationTypeNames.Query)]
    public class ComputedQuery
    {
        public IQueryable<ComputedRoot> GetRoots([Service] DbContext context) => context.Query<ComputedRoot>();
    }
}
