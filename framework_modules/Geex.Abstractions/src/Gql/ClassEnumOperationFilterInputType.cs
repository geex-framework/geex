using HotChocolate.Data.Filters;

namespace Geex.Gql
{
    public class ClassEnumOperationFilterInputType<TEnum> : EnumOperationFilterInputType<TEnum>
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            base.Configure(descriptor);
        }
    }
}
