using HotChocolate.Types;
using MongoDB.Bson.Serialization;

namespace Geex.Extensions.Authentication.Core.Entities;

public partial class UserSession
{
    public class UserSessionBsonConfig : BsonConfig<UserSession>
    {
        protected override void Map(BsonClassMap<UserSession> map, BsonIndexConfig<UserSession> indexConfig)
        {
            map.AutoMap();
            indexConfig.MapEntityDefaultIndex();
            indexConfig.MapIndex(
                builder => builder.Combine(
                    builder.Ascending(x => x.UserId),
                    builder.Ascending(x => x.LoginProvider)),
                options => { options.Unique = true; options.Background = true; });
        }
    }

    public class UserSessionGqlConfig : GqlConfig.Object<UserSession>
    {
        protected override void Configure(IObjectTypeDescriptor<UserSession> descriptor)
        {
            descriptor.BindFieldsImplicitly();
            base.Configure(descriptor);
        }
    }
}
