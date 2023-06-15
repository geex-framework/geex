using HotChocolate.Types;
using x_Org_x.x_Proj_x.x_Mod_x.Core.Aggregates.x_Aggregate_xs;

namespace x_Org_x.x_Proj_x.x_Mod_x.Core.GqlSchemas.x_Aggregate_xs.Types
{
    public class x_Aggregate_xGqlType : ObjectType<x_Aggregate_x>
    {
        protected override void Configure(IObjectTypeDescriptor<x_Aggregate_x> descriptor)
        {
            // Implicitly binding all fields, if you want to bind fields explicitly, read more about hot chocolate
            descriptor.BindFieldsImplicitly();
            descriptor.ConfigEntity();
            base.Configure(descriptor);
        }
    }
}
