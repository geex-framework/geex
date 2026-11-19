using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Geex.Extensions.Backups.Core.Entities;

public partial class Backup
{
    public class BackupBsonConfig : BsonConfig<Backup>
    {
        protected override void Map(BsonClassMap<Backup> map, BsonIndexConfig<Backup> indexConfig)
        {
            map.AutoMap();
            indexConfig.MapEntityDefaultIndex();
            indexConfig.MapIndex(x => x.Ascending(y => y.DatabaseName).Descending(y => y.ScheduledAt),
                options => options.Unique = true);
        }
    }
}
