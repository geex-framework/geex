using Geex.Gql.GeexFeatures;
using Geex.Gql.Types;
using Geex.Tests.SchemaTests.TestEntities;

using HotChocolate;
using HotChocolate.Types;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Geex.Tests.SchemaTests
{
    [Collection(nameof(TestsCollection))]
    public class AutoBatchLoadTypeInterceptorTests : TestsBase
    {
        public AutoBatchLoadTypeInterceptorTests(TestApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public void GlobalEnabledShouldAutoAttachAutoBatchLoadToRootFields()
        {
            var schema = ScopedService.GetRequiredService<ISchema>();
            var queryType = schema.GetType<ObjectType>(nameof(Query));

            var pagedField = queryType.Fields.First(x => x.Name == nameof(AutoBatchLoadTestQuery.AutoBatchLoadPaged).ToCamelCase());
            pagedField.AutoBatchLoadEnabled.ShouldBeTrue();

            var listField = queryType.Fields.First(x => x.Name == nameof(AutoBatchLoadTestQuery.AutoBatchLoadList).ToCamelCase());
            listField.AutoBatchLoadEnabled.ShouldBeTrue();

            var byIdField = queryType.Fields.First(x => x.Name == nameof(AutoBatchLoadTestQuery.AutoBatchLoadById).ToCamelCase());
            byIdField.AutoBatchLoadEnabled.ShouldBeTrue();
        }

        [Fact]
        public void UseAutoBatchLoadFalseShouldOverrideGlobalAutoAttach()
        {
            var schema = ScopedService.GetRequiredService<ISchema>();
            var queryType = schema.GetType<ObjectType>(nameof(Query));
            var optOutField = queryType.Fields.First(x => x.Name == nameof(AutoBatchLoadTestQuery.AutoBatchLoadOptOut).ToCamelCase());

            optOutField.AutoBatchLoadEnabled.ShouldBeFalse();
        }

        [Fact]
        public void ExplicitUseAutoBatchLoadTrueShouldAttachWhenConfigured()
        {
            var schema = ScopedService.GetRequiredService<ISchema>();
            var queryType = schema.GetType<ObjectType>(nameof(Query));
            var pagedField = queryType.Fields.First(x => x.Name == nameof(AutoBatchLoadTestQuery.AutoBatchLoadPaged).ToCamelCase());

            pagedField.GeexFeatures.AutoBatchLoadFeature.ShouldNotBeNull();
            pagedField.GeexFeatures.AutoBatchLoadFeature!.Enabled.ShouldBeTrue();
            pagedField.AutoBatchLoadEnabled.ShouldBeTrue();
        }
    }
}
