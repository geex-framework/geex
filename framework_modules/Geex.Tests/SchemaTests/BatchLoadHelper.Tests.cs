using Geex.Tests.SchemaTests.TestEntities;

using MongoDB.Entities.Utilities;

using Shouldly;

namespace Geex.Tests.SchemaTests
{
    public class BatchLoadHelperTests
    {
        private sealed class ResettableLazyNavigationHolder
        {
            public ResettableLazy<AutoBatchLoadTestEntity> Related { get; set; } = default!;
        }

        [Fact]
        public void IsLazyQueryNavigationPropertyShouldDetectQueryableAndLazy()
        {
            var childrenProperty = typeof(AutoBatchLoadTestEntity).GetProperty(nameof(AutoBatchLoadTestEntity.Children))!;
            var firstChildProperty = typeof(AutoBatchLoadTestEntity).GetProperty(nameof(AutoBatchLoadTestEntity.FirstChild))!;
            var thisIdProperty = typeof(AutoBatchLoadTestEntity).GetProperty(nameof(AutoBatchLoadTestEntity.ThisId))!;

            childrenProperty.IsLazyQueryNavigationProperty().ShouldBeTrue();
            firstChildProperty.IsLazyQueryNavigationProperty().ShouldBeTrue();
            thisIdProperty.IsLazyQueryNavigationProperty().ShouldBeFalse();
        }

        [Fact]
        public void TryGetRelatedEntityTypeShouldDetectResettableLazy()
        {
            var relatedProperty = typeof(ResettableLazyNavigationHolder).GetProperty(nameof(ResettableLazyNavigationHolder.Related))!;

            relatedProperty.TryGetRelatedEntityType(out var relatedType).ShouldBeTrue();
            relatedType.ShouldBe(typeof(AutoBatchLoadTestEntity));
        }

        [Fact]
        public void IsRegisteredLazyQueryNavigationShouldDetectConfiguredProperties()
        {
            var childrenProperty = typeof(AutoBatchLoadTestEntity).GetProperty(nameof(AutoBatchLoadTestEntity.Children))!;
            var thisIdProperty = typeof(AutoBatchLoadTestEntity).GetProperty(nameof(AutoBatchLoadTestEntity.ThisId))!;

            BatchLoadHelper.IsRegisteredLazyQueryNavigation(typeof(AutoBatchLoadTestEntity), childrenProperty).ShouldBeTrue();
            BatchLoadHelper.IsRegisteredLazyQueryNavigation(typeof(AutoBatchLoadTestEntity), thisIdProperty).ShouldBeFalse();
        }

        [Fact]
        public void EnsurePathShouldBuildNestedBatchLoadConfig()
        {
            var config = new BatchLoadConfig();
            var c1Property = typeof(AutoBatchLoadTestEntity).GetProperty(nameof(AutoBatchLoadTestEntity.Children))!;
            var c2Property = typeof(AutoBatchLoadChildEntity).GetProperty(nameof(AutoBatchLoadChildEntity.FirstChild))!;

            var childConfig = config.GetOrAddSubConfig(c1Property);
            childConfig.GetOrAddSubConfig(c2Property);

            config.ContainsSubConfig(c1Property).ShouldBeTrue();
            config.GetOrAddSubConfig(c1Property).ContainsSubConfig(c2Property).ShouldBeTrue();
        }
    }
}
