using Geex.Extensions.Authentication;
using Geex.Extensions.BlobStorage;
using Geex.Extensions.Identity;
using HotChocolate.Types;
using MongoDB.Bson.Serialization;

namespace Geex.Extensions.Identity.Core.Entities;

public partial class User
{
    public class IUserGqlConfig : GqlConfig.Interface<IUser>
    {
        protected override void Configure(IInterfaceTypeDescriptor<IUser> descriptor)
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Field(x => x.Id);
            descriptor.Field(x => x.CreatedOn);
            descriptor.Field(x => x.ModifiedOn);
            descriptor.Field(x => x.Username);
            descriptor.Field(x => x.Nickname);
            descriptor.Field(x => x.Email);
            descriptor.Field(x => x.PhoneNumber);
            descriptor.Field(x => x.IsEnable);
            descriptor.Field(x => x.RoleIds);
            descriptor.Field(x => x.OrgCodes);
            descriptor.Field(x => x.Permissions);
            descriptor.Field(x => x.Claims);
            descriptor.Field(x => x.ExternalLogins);
            descriptor.Field(x => x.Orgs);
            descriptor.Field(x => x.AvatarFile).Type<InterfaceType<IBlobObject>>();
            descriptor.Field(x => x.AvatarFileId);
            descriptor.Field(x => x.Roles);
            descriptor.Field(x => x.RoleNames);
            descriptor.Field(x => x.TenantCode);
            descriptor.IgnoreMethods();
        }
    }

    public class UserBsonConfig : BsonConfig<User>
    {
        protected override void Map(BsonClassMap<User> map, BsonIndexConfig<User> indexConfig)
        {
            map.Inherit<IAuthUser>();
            map.Inherit<IUser>();
            map.SetIsRootClass(true);
            map.AutoMap();
            indexConfig.MapEntityDefaultIndex();
            indexConfig.MapIndex(x => x.Ascending(y => y.Email), options => { options.Background = true; });
            indexConfig.MapIndex(x => x.Ascending(y => y.Username), options => { options.Background = true; });
            indexConfig.MapIndex(x => x.Ascending(y => y.PhoneNumber), options => { options.Background = true; });
        }
    }

    public class UserGqlConfig : GqlConfig.Object<User>
    {
        /// <inheritdoc />
        protected override void Configure(IObjectTypeDescriptor<User> descriptor)
        {
            descriptor.Implements<InterfaceType<IUser>>();
            descriptor.BindFieldsImplicitly();
            descriptor.Field(x => x.AvatarFile).Type<InterfaceType<IBlobObject>>();
            descriptor.Ignore(x => x.Password);
            //descriptor.Field(x => x.UserName);
            //descriptor.Field(x => x.IsEnable);
            //descriptor.Field(x => x.Email);
            //descriptor.Field(x => x.PhoneNumber);
            //descriptor.Field(x => x.Roles);
            //descriptor.Field(x => x.Orgs);
            descriptor.Field(x => x.Claims).UseFiltering<UserClaim>(x => { x.Field(y => y.ClaimType); });
            descriptor.Field(x => x.ExternalLogins);
            descriptor.Field(x => x.Orgs);
            //descriptor.Ignore(x => x.Claims);
            //descriptor.Ignore(x => x.AuthorizedPermissions);
        }
    }
}
