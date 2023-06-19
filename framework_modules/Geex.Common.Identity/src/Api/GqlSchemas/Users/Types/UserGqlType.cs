using Geex.Common.Abstraction.Entities;
using Geex.Common.Identity.Api.Aggregates.Users;
using Geex.Common.Identity.Core.Aggregates.Users;

using HotChocolate.Types;

namespace Geex.Common.Identity.Api.GqlSchemas.Users.Types
{
    public class UserGqlType : ObjectType<User>
    {
        protected override void Configure(IObjectTypeDescriptor<User> descriptor)
        {
            descriptor.AuthorizeFieldsImplicitly();
            descriptor.BindFieldsImplicitly();
            descriptor.Implements<InterfaceType<IUser>>();
            descriptor.ConfigEntity();
            //descriptor.Field(x => x.UserName);
            //descriptor.Field(x => x.IsEnable);
            //descriptor.Field(x => x.Email);
            //descriptor.Field(x => x.PhoneNumber);
            //descriptor.Field(x => x.Roles);
            //descriptor.Field(x => x.Orgs);
            descriptor.Field(x => x.Claims).UseFiltering<UserClaim>(x =>
            {
                x.Field(y => y.ClaimType);
            });
            //descriptor.Ignore(x => x.Claims);
            //descriptor.Ignore(x => x.AuthorizedPermissions);
            base.Configure(descriptor);
        }
    }
}
