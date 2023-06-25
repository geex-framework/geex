using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Bson;
using Geex.Common.Abstraction.Entities;
using Geex.Common.Identity.Core.Aggregates.Users;
using HotChocolate.Types;
using MongoDB.Bson.Serialization;

namespace Geex.Common.Identity.Core.EntityMapConfigs.Users
{
    public class UserEntityConfig : EntityConfig<User>
    {
        protected override void Map(BsonClassMap<User> map)
        {
            map.Inherit<IUser>();
            map.SetIsRootClass(true);
            map.AutoMap();
        }

        /// <inheritdoc />
        protected override void Configure(IObjectTypeDescriptor<User> descriptor)
        {
            descriptor.Implements<InterfaceType<IUser>>();
            descriptor.AuthorizeFieldsImplicitly();
            descriptor.BindFieldsImplicitly();
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
        }
    }
}
