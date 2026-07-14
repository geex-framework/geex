using HotChocolate.Types;
using MongoDB.Bson.Serialization;

namespace Geex.Extensions.Identity.Core.Entities;

public partial class UserLogin
{
    public class UserLoginBsonConfig : BsonConfig<UserLogin>
    {
        protected override void Map(BsonClassMap<UserLogin> map, BsonIndexConfig<UserLogin> indexConfig)
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

    public class UserLoginGqlConfig : GqlConfig.Object<UserLogin>
    {
        protected override void Configure(IObjectTypeDescriptor<UserLogin> descriptor)
        {
            descriptor.BindFieldsImplicitly();
        }
    }
}
