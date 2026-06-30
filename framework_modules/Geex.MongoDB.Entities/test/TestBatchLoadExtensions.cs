using System.Linq;
using System.Reflection;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MongoDB.Entities.Tests.Models;
using MongoDB.Entities.Utilities;

using Shouldly;

namespace MongoDB.Entities.Tests
{
    [TestClass]
    public class TestBatchLoadExtensions
    {
        private static readonly Type EntityType = typeof(BatchLoadEntity);

        [TestMethod]
        public void register_batch_load_should_be_idempotent()
        {
            var childrenProperty = typeof(BatchLoadEntity).GetProperty(nameof(BatchLoadEntity.Children))!;

            var config = new BatchLoadConfig();
            config.RegisterBatchLoad(childrenProperty, EntityType);
            config.RegisterBatchLoad(childrenProperty, EntityType);

            config.SubBatchLoadConfigs.Count.ShouldBe(1);
            config.SubBatchLoadConfigs.ContainsKey(childrenProperty).ShouldBeTrue();
        }

        [TestMethod]
        public void register_batch_load_should_throw_for_unregistered_navigation()
        {
            var scalarProperty = typeof(BatchLoadEntity).GetProperty(nameof(BatchLoadEntity.ThisId))!;

            var config = new BatchLoadConfig();
            Should.Throw<BatchLoadException>(() => config.RegisterBatchLoad(scalarProperty, EntityType));
        }

        [TestMethod]
        public void apply_selection_batch_load_should_add_new_paths()
        {
            var childrenProperty = typeof(BatchLoadEntity).GetProperty(nameof(BatchLoadEntity.Children))!;

            var manual = new BatchLoadConfig();
            var selection = new BatchLoadConfig();
            selection.RegisterBatchLoad(childrenProperty, EntityType);

            manual.ApplySelectionBatchLoad(selection);

            manual.SubBatchLoadConfigs.ContainsKey(childrenProperty).ShouldBeTrue();
        }

        [TestMethod]
        public void apply_selection_batch_load_should_not_remove_manual_only_paths()
        {
            var childrenProperty = typeof(BatchLoadEntity).GetProperty(nameof(BatchLoadEntity.Children))!;
            var firstChildProperty = typeof(BatchLoadEntity).GetProperty(nameof(BatchLoadEntity.FirstChild))!;

            var manual = new BatchLoadConfig();
            manual.RegisterBatchLoad(childrenProperty, EntityType).RegisterBatchLoad(firstChildProperty, EntityType);

            var selection = new BatchLoadConfig();
            selection.RegisterBatchLoad(childrenProperty, EntityType);

            manual.ApplySelectionBatchLoad(selection);

            manual.RegisterBatchLoad(childrenProperty, EntityType).SubBatchLoadConfigs.ContainsKey(firstChildProperty).ShouldBeTrue();
        }

        [TestMethod]
        public void apply_selection_batch_load_should_supplement_manual_partial_paths()
        {
            var childrenProperty = typeof(BatchLoadEntity).GetProperty(nameof(BatchLoadEntity.Children))!;
            var firstChildProperty = typeof(BatchLoadEntity).GetProperty(nameof(BatchLoadEntity.FirstChild))!;

            var manual = new BatchLoadConfig();
            manual.RegisterBatchLoad(childrenProperty, EntityType);

            var selection = new BatchLoadConfig();
            selection.RegisterBatchLoad(childrenProperty, EntityType).RegisterBatchLoad(firstChildProperty, EntityType);

            manual.ApplySelectionBatchLoad(selection);

            manual.RegisterBatchLoad(childrenProperty, EntityType).SubBatchLoadConfigs.ContainsKey(firstChildProperty).ShouldBeTrue();
        }

        [TestMethod]
        public void apply_selection_batch_load_duplicate_path_should_be_idempotent()
        {
            var childrenProperty = typeof(BatchLoadEntity).GetProperty(nameof(BatchLoadEntity.Children))!;
            var firstChildProperty = typeof(BatchLoadEntity).GetProperty(nameof(BatchLoadEntity.FirstChild))!;

            var manual = new BatchLoadConfig();
            manual.RegisterBatchLoad(childrenProperty, EntityType).RegisterBatchLoad(firstChildProperty, EntityType);

            var selection = new BatchLoadConfig();
            selection.RegisterBatchLoad(childrenProperty, EntityType).RegisterBatchLoad(firstChildProperty, EntityType);

            manual.ApplySelectionBatchLoad(selection);

            manual.SubBatchLoadConfigs.Count.ShouldBe(1);
            manual.RegisterBatchLoad(childrenProperty, EntityType).SubBatchLoadConfigs.Count.ShouldBe(1);
        }
    }
}
