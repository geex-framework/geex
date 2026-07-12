using Geex.Extensions.Authentication;
using Geex.Extensions.Identity;
using HotChocolate.Types;
using MongoDB.Bson.Serialization;

namespace Geex.Extensions.Identity.Core.Entities;

public partial class UserExternalLogin
{
    public class UserExternalLoginBsonConfig : BsonConfig<UserExternalLogin>
    {
        protected override void Map(BsonClassMap<UserExternalLogin> map, BsonIndexConfig<UserExternalLogin> indexConfig)
        {
            map.AutoMap();
            indexConfig.MapEntityDefaultIndex();
            indexConfig.MapIndex(
                builder => builder.Combine(
                    builder.Ascending(x => x.TenantCode),
                    builder.Ascending(x => x.LoginProvider),
                    builder.Ascending(x => x.LoginProviderId)),
                options =>
                {
                    options.Unique = true;
                    options.Background = true;
                });
            indexConfig.MapIndex(x => x.Ascending(y => y.UserId), options => { options.Background = true; });
        }
    }

    public class UserExternalLoginGqlConfig : GqlConfig.Object<UserExternalLogin>
    {
        protected override void Configure(IObjectTypeDescriptor<UserExternalLogin> descriptor)
        {
            descriptor.BindFieldsImplicitly();
        }
    }
}
