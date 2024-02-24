using HotChocolate.Data;
using HotChocolate.Data.Filters;

namespace Geex.Common;

public class GeexFilterConvention : FilterConvention
{
    protected override void Configure(IFilterConventionDescriptor descriptor)
    {
        descriptor.Provider(new GeexQueryablePostFilterProvider(y => y.AddDefaultFieldHandlers()));
        descriptor.ArgumentName("filter");
        descriptor.AddDefaults();
    }
}