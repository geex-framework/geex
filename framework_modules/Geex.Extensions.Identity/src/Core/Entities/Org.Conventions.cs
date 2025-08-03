using HotChocolate.Types;
using MongoDB.Bson.Serialization;

namespace Geex.Extensions.Identity.Core.Entities;

public partial class Org
{
    public class OrgBsonConfig : BsonConfig<Org>
    {
        protected override void Map(BsonClassMap<Org> map, BsonIndexConfig<Org> indexConfig)
        {
            map.Inherit<IOrg>();
            map.AutoMap();
            indexConfig.MapEntityDefaultIndex();
            indexConfig.MapIndex(x => x.Ascending(y => y.Code), options => options.Background = true);
            indexConfig.MapIndex(x => x.Ascending(y => y.Name), options => options.Background = true);
            indexConfig.MapIndex(x => x.Ascending(y => y.OrgType), options => options.Background = true);
        }
    }

    public class OrgGqlConfig : GqlConfig.Object<Org>
    {
        /// <inheritdoc />
        protected override void Configure(IObjectTypeDescriptor<Org> descriptor)
        {
            descriptor.BindFieldsImplicitly();
            //descriptor.Field(x => x.Users).Type<ListType<UserType>>().Resolve(x=>x.ToString());
            //descriptor.Field(x => x.Code);
            //descriptor.Field(x => x.Name);
            //descriptor.Field(x => x.OrgType);
            //descriptor.Field(x => x.AllSubOrgCodes);
            //descriptor.Field(x => x.DirectSubOrgCodes);
            //descriptor.Field(x => x.AllSubOrgs);
            //descriptor.Field(x => x.DirectSubOrgs);
            //descriptor.Field(x => x.ParentOrgCode);
            //descriptor.Field(x => x.ParentOrg);
            //descriptor.Field(x => x.AllParentOrgCodes);
            //descriptor.Field(x => x.AllParentOrgs);
        }
    }
}
