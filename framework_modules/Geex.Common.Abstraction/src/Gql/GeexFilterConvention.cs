using HotChocolate.Data;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Sorting;

namespace Geex.Common;

public class GeexFilterConvention : FilterConvention
{
    protected override void Configure(IFilterConventionDescriptor descriptor)
    {
        descriptor.Provider(new GeexQueryablePostFilterProvider(y => y.AddDefaultFieldHandlers()));
    }
}