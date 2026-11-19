using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson.Serialization;
using MongoDB.Entities.Utilities;
using Shouldly;

namespace MongoDB.Entities.Tests;

[TestClass]
public class TestLazyQueryMetadata
{
    [TestMethod]
    public void batch_load_registration_initializes_a_cold_protected_constructor()
    {
        ProtectedEntity.ConstructorCalls.ShouldBe(0);
        var property = typeof(ProtectedEntity).GetProperty(nameof(ProtectedEntity.Children))!;
        var config = new BatchLoadConfig();

        config.RegisterBatchLoad(property, typeof(ProtectedEntity));
        config.RegisterBatchLoad(property, typeof(ProtectedEntity));

        config.SubBatchLoadConfigs.Count.ShouldBe(1);
        ProtectedEntity.ConstructorCalls.ShouldBe(1);
    }

    [TestMethod]
    public void mapped_interface_initializes_its_concrete_entity()
    {
        DB.InterfaceCache[typeof(IColdEntity)] = new BsonClassMap<InterfaceEntity>();
        InterfaceEntity.ConstructorCalls.ShouldBe(0);
        using var context = new DbContext();

        _ = context.Query<IColdEntity>().BatchLoad(x => x.Children);

        LazyQueryMetadataRegistry.IsRegistered(typeof(IColdEntity), nameof(IColdEntity.Children)).ShouldBeTrue();
        LazyQueryMetadataRegistry.IsRegistered(typeof(InterfaceEntity), nameof(IColdEntity.Children)).ShouldBeTrue();

        InterfaceEntity.ConstructorCalls.ShouldBe(1);
    }

    [TestMethod]
    public void single_navigation_can_be_the_first_batch_load_path()
    {
        var property = typeof(SingleEntity).GetProperty(nameof(SingleEntity.FirstChild))!;
        var config = new BatchLoadConfig();

        config.RegisterBatchLoad(property, typeof(SingleEntity));

        config.SubBatchLoadConfigs.Count.ShouldBe(1);
        SingleEntity.ConstructorCalls.ShouldBe(1);
    }

    [TestMethod]
    public void unresolved_interface_can_be_resolved_after_mapping_is_registered()
    {
        LazyQueryMetadataRegistry.IsRegistered(typeof(ILateEntity), nameof(ILateEntity.Children)).ShouldBeFalse();
        DB.InterfaceCache[typeof(ILateEntity)] = new BsonClassMap<LateEntity>();

        LazyQueryMetadataRegistry.IsRegistered(typeof(ILateEntity), nameof(ILateEntity.Children)).ShouldBeTrue();
    }

    [TestMethod]
    public void inherited_navigation_is_registered_for_the_concrete_derived_type()
    {
        using var context = new DbContext();
        var query = context.Query<DerivedEntity>()
            .BatchLoad(x => x.Children)
            .ThenBatchLoad(x => x.Leaves);
        var config = ((ICachedDbContextQueryProvider)query.Provider).BatchLoadConfig;

        DerivedEntity.ConstructorCalls.ShouldBe(1);
        DerivedChild.ConstructorCalls.ShouldBe(1);
        var root = config.SubBatchLoadConfigs.Single().Value;
        root.DeclaringEntityType.ShouldBe(typeof(DerivedEntity));
        root.Children.SubBatchLoadConfigs.Single().Value.DeclaringEntityType.ShouldBe(typeof(DerivedChild));
    }

    [TestMethod]
    public void another_implementation_does_not_make_an_unconfigured_navigation_batchable()
    {
        LazyQueryMetadataRegistry.Register(typeof(ConfiguredImplementation), nameof(ISharedEntity.Children));
        DB.InterfaceCache[typeof(ISharedEntity)] = new BsonClassMap<UnconfiguredImplementation>();

        LazyQueryMetadataRegistry.IsRegistered(typeof(UnconfiguredImplementation), nameof(ISharedEntity.Children)).ShouldBeFalse();
        LazyQueryMetadataRegistry.IsRegistered(typeof(ISharedEntity), nameof(ISharedEntity.Children)).ShouldBeFalse();
        UnconfiguredImplementation.ConstructorCalls.ShouldBe(1);
    }

    [TestMethod]
    public void unconfigured_queryable_is_rejected_and_the_negative_result_is_cached()
    {
        var property = typeof(UnconfiguredEntity).GetProperty(nameof(UnconfiguredEntity.Children))!;
        var config = new BatchLoadConfig();

        Should.Throw<BatchLoadException>(() => config.RegisterBatchLoad(property, typeof(UnconfiguredEntity)))
            .Message.ShouldContain("未通过 ConfigLazyQuery 注册 LazyQuery");
        LazyQueryMetadataRegistry.IsRegistered(typeof(UnconfiguredEntity), property.Name).ShouldBeFalse();

        config.SubBatchLoadConfigs.ShouldBeEmpty();
        UnconfiguredEntity.ConstructorCalls.ShouldBe(1);
    }

    [TestMethod]
    public async Task simultaneous_first_reads_initialize_the_type_once()
    {
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var readers = Enumerable.Range(0, 24).Select(_ => Task.Run(async () =>
        {
            await start.Task;
            return LazyQueryMetadataRegistry.IsRegistered(typeof(ConcurrentEntity), nameof(ConcurrentEntity.Children));
        })).ToArray();

        start.SetResult();
        var results = await Task.WhenAll(readers).WaitAsync(TimeSpan.FromSeconds(10));

        results.ShouldAllBe(value => value);
        ConcurrentEntity.ConstructorCalls.ShouldBe(1);
    }

    [TestMethod]
    public void failed_constructor_does_not_publish_its_partial_registration_or_retry()
    {
        var first = Should.Throw<InvalidOperationException>(() =>
            LazyQueryMetadataRegistry.IsRegistered(typeof(FailingEntity), nameof(FailingEntity.Children)));
        var second = Should.Throw<InvalidOperationException>(() =>
            LazyQueryMetadataRegistry.IsRegistered(typeof(FailingEntity), nameof(FailingEntity.Children)));

        first.Message.ShouldContain(typeof(FailingEntity).FullName!);
        first.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("cold constructor failed");
        second.ShouldBeSameAs(first);
        FailingEntity.ConstructorCalls.ShouldBe(1);
        LazyQueryMetadataRegistry.IsRegistered(typeof(IFailingEntity), nameof(IFailingEntity.Children)).ShouldBeFalse();
    }

    [TestMethod]
    public void recursive_metadata_initialization_fails_with_entity_context()
    {
        var error = Should.Throw<InvalidOperationException>(() =>
            LazyQueryMetadataRegistry.IsRegistered(typeof(RecursiveEntity), nameof(RecursiveEntity.Children)));

        error.Message.ShouldContain(typeof(RecursiveEntity).FullName!);
        error.InnerException.ShouldBeOfType<InvalidOperationException>();
        RecursiveEntity.ConstructorCalls.ShouldBe(1);
    }

    [TestMethod]
    public void missing_parameterless_constructor_reports_the_required_configuration()
    {
        var error = Should.Throw<InvalidOperationException>(() =>
            LazyQueryMetadataRegistry.IsRegistered(typeof(MissingConstructorEntity), nameof(MissingConstructorEntity.Children)));

        error.Message.ShouldContain(typeof(MissingConstructorEntity).FullName!);
        error.Message.ShouldContain("parameterless constructor");
    }

    [TestMethod]
    public void explicit_metadata_does_not_require_an_additional_constructor()
    {
        LazyQueryMetadataRegistry.Register(typeof(ExplicitEntity), nameof(ExplicitEntity.Children));

        LazyQueryMetadataRegistry.IsRegistered(typeof(ExplicitEntity), nameof(ExplicitEntity.Children)).ShouldBeTrue();
    }

    [TestMethod]
    public void initialized_instance_keeps_its_existing_registration_without_a_probe()
    {
        _ = new WarmEntity();

        LazyQueryMetadataRegistry.IsRegistered(typeof(WarmEntity), nameof(WarmEntity.Children)).ShouldBeTrue();

        WarmEntity.ConstructorCalls.ShouldBe(1);
    }

    [TestMethod]
    public void metadata_initialization_does_not_evaluate_or_share_instance_data_sources()
    {
        LazyQueryMetadataRegistry.IsRegistered(typeof(BoundEntity), nameof(BoundEntity.Children)).ShouldBeTrue();
        BoundEntity.SourceCalls.ShouldBe(0);
        var children = new[] { new BoundChild { ParentId = "a" }, new BoundChild { ParentId = "b" } };
        var first = new BoundEntity("a", children);
        var second = new BoundEntity("b", children);

        first.Children.Single().ShouldBeSameAs(children[0]);
        second.Children.Single().ShouldBeSameAs(children[1]);

        BoundEntity.SourceCalls.ShouldBe(2);
    }

    public class ProtectedEntity : EntityBase<ProtectedEntity>
    {
        public static int ConstructorCalls;
        protected ProtectedEntity()
        {
            ConstructorCalls++;
            ConfigLazyQuery(x => x.Children, x => x.ParentId == Id, roots => x => roots.Select(r => r.Id).Contains(x.ParentId));
        }
        public string ParentId { get; set; } = "";
        public IQueryable<ProtectedEntity> Children => LazyQuery(() => Children);
    }

    public interface IColdEntity : IEntityBase { IQueryable<InterfaceEntity> Children { get; } }
    public class InterfaceEntity : EntityBase<InterfaceEntity>, IColdEntity
    {
        public static int ConstructorCalls;
        private InterfaceEntity()
        {
            ConstructorCalls++;
            ConfigLazyQuery(x => x.Children, x => x.ParentId == Id, roots => x => roots.Select(r => r.Id).Contains(x.ParentId));
        }
        public string ParentId { get; set; } = "";
        public IQueryable<InterfaceEntity> Children => LazyQuery(() => Children);
    }

    public interface ILateEntity : IEntityBase { IQueryable<LateEntity> Children { get; } }

    public class SingleEntity : EntityBase<SingleEntity>
    {
        public static int ConstructorCalls;
        private SingleEntity()
        {
            ConstructorCalls++;
            ConfigLazyQuery(x => x.FirstChild, x => x.ParentId == Id, roots => x => roots.Select(r => r.Id).Contains(x.ParentId));
        }
        public string ParentId { get; set; } = "";
        public Lazy<SingleEntity> FirstChild => LazyQuery(() => FirstChild);
    }
    public class LateEntity : EntityBase<LateEntity>, ILateEntity
    {
        public LateEntity() => ConfigLazyQuery(x => x.Children, x => x.ParentId == Id, roots => x => roots.Select(r => r.Id).Contains(x.ParentId));
        public string ParentId { get; set; } = "";
        public IQueryable<LateEntity> Children => LazyQuery(() => Children);
    }

    public abstract class InheritedEntity : EntityBase<InheritedEntity>
    {
        protected InheritedEntity() => ConfigLazyQuery(x => x.Children, x => x.ParentId == Id, roots => x => roots.Select(r => r.Id).Contains(x.ParentId));
        public string ParentId { get; set; } = "";
        public IQueryable<DerivedChild> Children => LazyQuery(() => Children);
    }
    public class DerivedEntity : InheritedEntity
    {
        public static int ConstructorCalls;
        private DerivedEntity() => ConstructorCalls++;
    }
    public abstract class InheritedChild : EntityBase<InheritedChild>
    {
        protected InheritedChild() => ConfigLazyQuery(x => x.Leaves, x => x.ParentId == Id, roots => x => roots.Select(r => r.Id).Contains(x.ParentId));
        public string ParentId { get; set; } = "";
        public IQueryable<BoundChild> Leaves => LazyQuery(() => Leaves);
    }
    public class DerivedChild : InheritedChild
    {
        public static int ConstructorCalls;
        private DerivedChild() => ConstructorCalls++;
    }

    public interface ISharedEntity : IEntityBase { IQueryable<BoundChild> Children { get; } }
    public class ConfiguredImplementation : EntityBase<ConfiguredImplementation>, ISharedEntity
    {
        public IQueryable<BoundChild> Children => LazyQuery(() => Children);
    }
    public class UnconfiguredImplementation : EntityBase<UnconfiguredImplementation>, ISharedEntity
    {
        public static int ConstructorCalls;
        public UnconfiguredImplementation() => ConstructorCalls++;
        public IQueryable<BoundChild> Children => Array.Empty<BoundChild>().AsQueryable();
    }
    public class UnconfiguredEntity : EntityBase<UnconfiguredEntity>
    {
        public static int ConstructorCalls;
        public UnconfiguredEntity() => ConstructorCalls++;
        public IQueryable<BoundChild> Children => Array.Empty<BoundChild>().AsQueryable();
    }

    public class ConcurrentEntity : EntityBase<ConcurrentEntity>
    {
        public static int ConstructorCalls;
        public ConcurrentEntity()
        {
            Interlocked.Increment(ref ConstructorCalls);
            ConfigLazyQuery(x => x.Children, x => x.ParentId == Id, roots => x => roots.Select(r => r.Id).Contains(x.ParentId));
        }
        public string ParentId { get; set; } = "";
        public IQueryable<ConcurrentEntity> Children => LazyQuery(() => Children);
    }
    public interface IFailingEntity : IEntityBase { IQueryable<FailingEntity> Children { get; } }
    public class FailingEntity : EntityBase<FailingEntity>, IFailingEntity
    {
        public static int ConstructorCalls;
        public FailingEntity()
        {
            ConstructorCalls++;
            ConfigLazyQuery(x => x.Children, x => x.ParentId == Id, roots => x => roots.Select(r => r.Id).Contains(x.ParentId));
            throw new InvalidOperationException("cold constructor failed");
        }
        public string ParentId { get; set; } = "";
        public IQueryable<FailingEntity> Children => LazyQuery(() => Children);
    }
    public class RecursiveEntity : EntityBase<RecursiveEntity>
    {
        public static int ConstructorCalls;
        public RecursiveEntity()
        {
            ConstructorCalls++;
            ConfigLazyQuery(x => x.Children, x => x.ParentId == Id, roots => x => roots.Select(r => r.Id).Contains(x.ParentId));
            LazyQueryMetadataRegistry.IsRegistered(typeof(RecursiveEntity), nameof(Children));
        }
        public string ParentId { get; set; } = "";
        public IQueryable<RecursiveEntity> Children => LazyQuery(() => Children);
    }
    public class MissingConstructorEntity : EntityBase<MissingConstructorEntity>
    {
        public MissingConstructorEntity(string id) { Id = id; }
        public IQueryable<MissingConstructorEntity> Children => LazyQuery(() => Children);
    }
    public class ExplicitEntity : EntityBase<ExplicitEntity>
    {
        public ExplicitEntity(string id) { Id = id; }
        public IQueryable<ExplicitEntity> Children => LazyQuery(() => Children);
    }
    public class WarmEntity : EntityBase<WarmEntity>
    {
        public static int ConstructorCalls;
        public WarmEntity()
        {
            ConstructorCalls++;
            ConfigLazyQuery(x => x.Children, x => x.ParentId == Id, roots => x => roots.Select(r => r.Id).Contains(x.ParentId));
        }
        public string ParentId { get; set; } = "";
        public IQueryable<WarmEntity> Children => LazyQuery(() => Children);
    }
    public class BoundEntity : EntityBase<BoundEntity>
    {
        public static int SourceCalls;
        private BoundChild[] _children = null!;
        private string _key = "";
        private BoundEntity() => ConfigLazyQuery(x => x.Children, x => x.ParentId == _key,
            roots => x => roots.Select(r => r._key).Contains(x.ParentId), () =>
            {
                Interlocked.Increment(ref SourceCalls);
                return _children.AsQueryable();
            });
        public BoundEntity(string key, BoundChild[] children) : this() { _key = key; _children = children; }
        public IQueryable<BoundChild> Children => LazyQuery(() => Children);
    }
    public class BoundChild : EntityBase<BoundChild> { public string ParentId { get; set; } = ""; }
}
