using Geex.Abstractions;
using Geex.Entities;
using Geex.Extensions.BlobStorage;
using HotChocolate.Types;
using MongoDB.Bson.Serialization;

namespace Geex.Extensions.Identity.Core.Entities;

public partial class User
{
    public class UserBsonConfig : BsonConfig<User>
    {
        protected override void Map(BsonClassMap<User> map, BsonIndexConfig<User> indexConfig)
        {
            map.Inherit<IUser>();
            map.SetIsRootClass(true);
            map.AutoMap();
            indexConfig.MapEntityDefaultIndex();
            indexConfig.MapIndex(x => x.Ascending(y => y.OpenId), options =>
            {
                options.Background = true;
                options.Sparse = true;
            });
            indexConfig.MapIndex(x => x.Ascending(y => y.Email), options => { options.Background = true; });
            indexConfig.MapIndex(x => x.Ascending(y => y.Username), options => { options.Background = true; });
            indexConfig.MapIndex(x => x.Hashed(y => y.LoginProvider), options =>
            {
                options.Background = true;
                options.Sparse = true;
            });
            indexConfig.MapIndex(x => x.Ascending(y => y.PhoneNumber), options => { options.Background = true; });
        }
    }

    public class UserGqlConfig : GqlConfig.Object<User>
    {
        /// <inheritdoc />
        protected override void Configure(IObjectTypeDescriptor<User> descriptor)
        {
            descriptor.Implements<InterfaceType<IUser>>();
            descriptor.AuthorizeFieldsImplicitly();
            descriptor.BindFieldsImplicitly();
            descriptor.Field(x => x.AvatarFile).Type<InterfaceType<IBlobObject>>();
            descriptor.Ignore(x => x.Password);
            descriptor.ConfigEntity();
            //descriptor.Field(x => x.UserName);
            //descriptor.Field(x => x.IsEnable);
            //descriptor.Field(x => x.Email);
            //descriptor.Field(x => x.PhoneNumber);
            //descriptor.Field(x => x.Roles);
            //descriptor.Field(x => x.Orgs);
            descriptor.Field(x => x.Claims).UseFiltering<UserClaim>(x => { x.Field(y => y.ClaimType); });
            //descriptor.Ignore(x => x.Claims);
            //descriptor.Ignore(x => x.AuthorizedPermissions);
        }
    }
}
