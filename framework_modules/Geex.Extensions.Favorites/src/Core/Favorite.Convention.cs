using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Geex.Extensions.Favorites.Core;

public partial class Favorite
{
    public class FavoriteBsonConfig : BsonConfig<Favorite>
    {
        protected override void Map(BsonClassMap<Favorite> map, BsonIndexConfig<Favorite> indexes)
        {
            map.AutoMap();
            indexes.MapEntityDefaultIndex();
            indexes.MapIndex(keys => keys.Ascending(x => x.TenantCode).Ascending(x => x.UserId)
                .Ascending(x => x.EntityType).Ascending(x => x.EntityId), options => options.Unique = true);
            indexes.MapIndex(keys => keys.Ascending(x => x.TenantCode).Ascending(x => x.UserId)
                .Ascending(x => x.EntityType).Descending(x => x.CreatedOn).Descending(x => x.Id));
        }
    }
}
