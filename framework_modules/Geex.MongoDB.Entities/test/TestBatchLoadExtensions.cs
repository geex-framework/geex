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
        public void apply_selection_overlay_should_add_new_paths()
        {
            var childrenProperty = typeof(BatchLoadEntity).GetProperty(nameof(BatchLoadEntity.Children))!;
            var manual = new BatchLoadConfig();
            var selection = new BatchLoadConfig();
            selection.GetOrAddSubConfig(childrenProperty);

            manual.ApplySelectionOverlay(selection);

            manual.ContainsSubConfig(childrenProperty).ShouldBeTrue();
        }

        [TestMethod]
        public void apply_selection_overlay_should_overwrite_manual_at_same_path()
        {
            var childrenProperty = typeof(BatchLoadEntity).GetProperty(nameof(BatchLoadEntity.Children))!;
            var firstChildProperty = typeof(BatchLoadEntity).GetProperty(nameof(BatchLoadEntity.FirstChild))!;

            var manual = new BatchLoadConfig();
            var manualChildren = manual.GetOrAddSubConfig(childrenProperty);
            manualChildren.GetOrAddSubConfig(firstChildProperty);

            var selection = new BatchLoadConfig();
            selection.GetOrAddSubConfig(childrenProperty);

            manual.ApplySelectionOverlay(selection);

            manual.ContainsSubConfig(childrenProperty).ShouldBeTrue();
            manual.GetOrAddSubConfig(childrenProperty).ContainsSubConfig(firstChildProperty).ShouldBeFalse();
        }

        [TestMethod]
        public void apply_selection_overlay_should_keep_manual_orphan_siblings()
        {
            var childrenProperty = typeof(BatchLoadEntity).GetProperty(nameof(BatchLoadEntity.Children))!;
            var firstChildProperty = typeof(BatchLoadEntity).GetProperty(nameof(BatchLoadEntity.FirstChild))!;

            var manual = new BatchLoadConfig();
            manual.GetOrAddSubConfig(firstChildProperty);

            var selection = new BatchLoadConfig();
            selection.GetOrAddSubConfig(childrenProperty);

            manual.ApplySelectionOverlay(selection);

            manual.ContainsSubConfig(childrenProperty).ShouldBeTrue();
            manual.ContainsSubConfig(firstChildProperty).ShouldBeTrue();
        }

        [TestMethod]
        public void apply_selection_overlay_should_keep_nested_manual_orphan()
        {
            var childrenProperty = typeof(BatchLoadEntity).GetProperty(nameof(BatchLoadEntity.Children))!;
            var firstChildProperty = typeof(BatchLoadEntity).GetProperty(nameof(BatchLoadEntity.FirstChild))!;

            var manual = new BatchLoadConfig();
            manual.GetOrAddSubConfig(childrenProperty).GetOrAddSubConfig(firstChildProperty);

            var selection = new BatchLoadConfig();
            selection.GetOrAddSubConfig(childrenProperty);

            manual.ApplySelectionOverlay(selection);

            manual.GetOrAddSubConfig(childrenProperty).ContainsSubConfig(firstChildProperty).ShouldBeTrue();
        }
    }
}
