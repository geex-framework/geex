using MongoDB.Bson.Serialization;
using MongoDB.Entities;

namespace Geex.Extensions.Authorization.Core.Casbin
{
    public class CasbinRule : EntityBase<CasbinRule>
    {
        public string PType { get; set; }
        public string V0 { get; set; }
        public string V1 { get; set; }
        public string V2 { get; set; }
        public string V3 { get; set; }
        public string V4 { get; set; }
        public string V5 { get; set; }

        public class CasbinRuleConfig : BsonConfig<CasbinRule>
        {
            /// <inheritdoc />
            protected override void Map(BsonClassMap<CasbinRule> map, BsonIndexConfig<CasbinRule> indexConfig)
            {
                map.SetIsRootClass(true);
                map.AutoMap();
                indexConfig.MapIndex(builder => builder.Descending(x => x.CreatedOn));
                indexConfig.MapIndex(x => x.Hashed(rule => rule.PType), options => options.Background = true);
                indexConfig.MapIndex(x => x.Hashed(rule => rule.V0), options => options.Background = true);
                indexConfig.MapIndex(x => x.Hashed(rule => rule.V1), options => options.Background = true);
                indexConfig.MapIndex(x => x.Hashed(rule => rule.V2), options => options.Background = true);
                indexConfig.MapIndex(x => x.Hashed(rule => rule.V3), options => options.Background = true);
                indexConfig.MapIndex(x => x.Hashed(rule => rule.V4), options => options.Background = true);
            }
        }
    }
}
