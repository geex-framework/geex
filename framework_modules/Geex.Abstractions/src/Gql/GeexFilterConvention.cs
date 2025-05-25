using HotChocolate.Data;
using HotChocolate.Data.Filters;

namespace Geex.Gql;

public class GeexFilterConvention : FilterConvention
{
    protected override void Configure(IFilterConventionDescriptor descriptor)
    {
        descriptor.AddDefaults();
        descriptor.ArgumentName("filter");
        descriptor.Provider(new GeexQueryablePostFilterProvider(y => y.AddDefaultFieldHandlers()));
    }
}