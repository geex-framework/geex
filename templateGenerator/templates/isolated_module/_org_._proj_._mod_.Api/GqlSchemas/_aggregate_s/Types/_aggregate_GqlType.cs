using _org_._proj_._mod_.Api.Aggregates._aggregate_s;
using HotChocolate.Types;

namespace _org_._proj_._mod_.Api.GqlSchemas._aggregate_s.Types
{
    public class _aggregate_GqlType : ObjectType<I_aggregate_>
    {
        protected override void Configure(IObjectTypeDescriptor<I_aggregate_> descriptor)
        {
            // Implicitly binding all fields, if you want to bind fields explicitly, read more about hot chocolate
            descriptor.BindFieldsImplicitly();
            base.Configure(descriptor);
        }
    }
}
