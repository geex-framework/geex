using HotChocolate.Types;

namespace Geex.Extensions.Authentication
{
    public class ResolveExternalLoginResultGqlConfig : GqlConfig.Object<ResolveExternalLoginResult>
    {
        protected override void Configure(IObjectTypeDescriptor<ResolveExternalLoginResult> descriptor)
        {
            descriptor.BindFieldsImplicitly();
            base.Configure(descriptor);
        }
    }
}
