using Geex.Common.Authentication.Domain;

using HotChocolate.Types;

namespace Geex.Common.Authentication.GqlSchemas.Types
{
    public class UserTokenGqlType : ObjectType<UserToken>
    {
        protected override void Configure(IObjectTypeDescriptor<UserToken> descriptor)
        {
            descriptor.BindFieldsImplicitly();
            descriptor.Ignore(x => x.Value);
            descriptor.Field("token").Resolve(x => x.Parent<UserToken>().Value);
            base.Configure(descriptor);
        }
    }
}
