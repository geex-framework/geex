using HotChocolate.Data.Filters;

namespace Geex.Common.Abstraction.Gql
{
    public class ClassEnumOperationFilterInputType<TEnum> : EnumOperationFilterInputType<TEnum>
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            base.Configure(descriptor);
        }
    }
}
