using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Entities;
using Geex.Common.Identity.Api.Aggregates.Roles;
using HotChocolate.Types;
using MongoDB.Bson.Serialization;

namespace Geex.Common.Identity.Core.EntityMapConfigs.Roles
{
    public class RoleEntityConfig : EntityConfig<Role>
    {
        protected override void Map(BsonClassMap<Role> map)
        {
            map.Inherit<IRole>();
            map.AutoMap();
        }

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
