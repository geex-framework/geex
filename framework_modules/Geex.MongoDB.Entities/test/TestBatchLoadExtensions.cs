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
        [TestMethod]
        public void register_batch_load_should_be_idempotent()
        {
            var childrenProperty = typeof(BatchLoadEntity).GetProperty(nameof(BatchLoadEntity.Children))!;

            var config = new BatchLoadConfig();
            config.RegisterBatchLoad(childrenProperty);
            config.RegisterBatchLoad(childrenProperty);

            config.SubBatchLoadConfigs.Count.ShouldBe(1);
            config.SubBatchLoadConfigs.ContainsKey(childrenProperty).ShouldBeTrue();
        }

        [TestMethod]
        public void apply_selection_batch_load_should_add_new_paths()
        {
            var childrenProperty = typeof(BatchLoadEntity).GetProperty(nameof(BatchLoadEntity.Children))!;

            var manual = new BatchLoadConfig();
            var selection = new BatchLoadConfig();
            selection.RegisterBatchLoad(childrenProperty);

            manual.ApplySelectionBatchLoad(selection);

            manual.SubBatchLoadConfigs.ContainsKey(childrenProperty).ShouldBeTrue();
        }

        [TestMethod]
        public void apply_selection_batch_load_should_not_remove_manual_only_paths()
        {
            var childrenProperty = typeof(BatchLoadEntity).GetProperty(nameof(BatchLoadEntity.Children))!;
            var firstChildProperty = typeof(BatchLoadEntity).GetProperty(nameof(BatchLoadEntity.FirstChild))!;

            var manual = new BatchLoadConfig();
            manual.RegisterBatchLoad(childrenProperty).RegisterBatchLoad(firstChildProperty);

            var selection = new BatchLoadConfig();
            selection.RegisterBatchLoad(childrenProperty);

            manual.ApplySelectionBatchLoad(selection);

            manual.RegisterBatchLoad(childrenProperty).SubBatchLoadConfigs.ContainsKey(firstChildProperty).ShouldBeTrue();
        }

        [TestMethod]
        public void apply_selection_batch_load_should_supplement_manual_partial_paths()
        {
            var childrenProperty = typeof(BatchLoadEntity).GetProperty(nameof(BatchLoadEntity.Children))!;
            var firstChildProperty = typeof(BatchLoadEntity).GetProperty(nameof(BatchLoadEntity.FirstChild))!;

            var manual = new BatchLoadConfig();
            manual.RegisterBatchLoad(childrenProperty);

            var selection = new BatchLoadConfig();
            selection.RegisterBatchLoad(childrenProperty).RegisterBatchLoad(firstChildProperty);

            manual.ApplySelectionBatchLoad(selection);

            manual.RegisterBatchLoad(childrenProperty).SubBatchLoadConfigs.ContainsKey(firstChildProperty).ShouldBeTrue();
        }

        [TestMethod]
        public void apply_selection_batch_load_duplicate_path_should_be_idempotent()
        {
            var childrenProperty = typeof(BatchLoadEntity).GetProperty(nameof(BatchLoadEntity.Children))!;
            var firstChildProperty = typeof(BatchLoadEntity).GetProperty(nameof(BatchLoadEntity.FirstChild))!;

            var manual = new BatchLoadConfig();
            manual.RegisterBatchLoad(childrenProperty).RegisterBatchLoad(firstChildProperty);

            var selection = new BatchLoadConfig();
            selection.RegisterBatchLoad(childrenProperty).RegisterBatchLoad(firstChildProperty);

            manual.ApplySelectionBatchLoad(selection);

            manual.SubBatchLoadConfigs.Count.ShouldBe(1);
            manual.RegisterBatchLoad(childrenProperty).SubBatchLoadConfigs.Count.ShouldBe(1);
        }
    }
}
