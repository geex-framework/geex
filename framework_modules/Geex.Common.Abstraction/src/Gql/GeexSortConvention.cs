using HotChocolate.Data;
using HotChocolate.Data.Sorting;

namespace Geex.Common.Abstraction.Gql;

public class GeexSortConvention : SortConvention
{
    protected override void Configure(ISortConventionDescriptor descriptor)
    {
        descriptor.Operation(DefaultSortOperations.Ascending).Name("ascend");
        descriptor.Operation(DefaultSortOperations.Descending).Name("descend");
        descriptor.BindRuntimeType<string, DefaultSortEnumType>();
        descriptor.DefaultBinding<DefaultSortEnumType>();
        descriptor.UseQueryableProvider();
        //descriptor.BindRuntimeType(descriptor);
    }
}