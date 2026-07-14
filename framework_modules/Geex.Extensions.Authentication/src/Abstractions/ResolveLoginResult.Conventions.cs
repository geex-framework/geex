using HotChocolate.Types;

namespace Geex.Extensions.Authentication
{
    public class ResolveLoginResultGqlConfig : GqlConfig.Object<ResolveLoginResult>
    {
        protected override void Configure(IObjectTypeDescriptor<ResolveLoginResult> descriptor)
        {
            descriptor.BindFieldsImplicitly();
            base.Configure(descriptor);
        }
    }
}
