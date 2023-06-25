using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Bson;
using Geex.Common.Abstraction.Entities;
using Geex.Common.Identity.Core.Aggregates.Orgs;
using HotChocolate.Types;
using MongoDB.Bson.Serialization;

namespace Geex.Common.Identity.Core.EntityMapConfigs.Orgs
{
    public class OrgEntityConfig : EntityConfig<Org>
    {
        protected override void Map(BsonClassMap<Org> map)
        {
            map.Inherit<IOrg>();
            map.AutoMap();
        }

        /// <inheritdoc />
        protected override void Configure(IObjectTypeDescriptor<Org> descriptor)
        {
            descriptor.AuthorizeFieldsImplicitly();
            descriptor.BindFieldsImplicitly();
            descriptor.ConfigEntity();
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
