using Geex.Abstractions;
using Geex.Abstractions.Entities;
using HotChocolate.Types;
using MongoDB.Bson.Serialization;

namespace Geex.Common.Identity.Core.Entities;

public partial class Role
{
    public class RoleBsonConfig : BsonConfig<Role>
    {
        protected override void Map(BsonClassMap<Role> map, BsonIndexConfig<Role> indexConfig)
        {
            map.Inherit<IRole>();
            map.AutoMap();
            indexConfig.MapEntityDefaultIndex();
            indexConfig.MapIndex(x => x.Ascending(y => y.Name), options => options.Background = true);
            indexConfig.MapIndex(x => x.Ascending(y => y.Code), options => options.Background = true);
        }
    }

    public class RoleGqlConfig : GqlConfig.Object<Role>
    {
        /// <inheritdoc />
        protected override void Configure(IObjectTypeDescriptor<Role> descriptor)
        {
            descriptor.BindFieldsImplicitly();
            //descriptor.Field(x => x.Users).Type<ListType<UserType>>().Resolve(x=>x.ToString());
            descriptor.ConfigEntity();
            descriptor.AuthorizeFieldsImplicitly();
        }
    }
}