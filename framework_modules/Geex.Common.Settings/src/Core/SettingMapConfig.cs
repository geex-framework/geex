using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Bson;
using Geex.Common.Settings.Abstraction;
using Geex.Common.Settings.Api.Aggregates.Settings;

using HotChocolate.Types;

using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Geex.Common.Settings.Core
{
    public class SettingEntityConfig : EntityConfig<Setting>
    {
        protected override void Map(BsonClassMap<Setting> map)
        {
            map.Inherit<ISetting>();
            map.AutoMap();
        }

        /// <inheritdoc />
        protected override void Configure(IObjectTypeDescriptor<Setting> descriptor)
        {
            descriptor.BindFieldsImplicitly();
            descriptor.Implements<InterfaceType<ISetting>>();
            descriptor.ConfigEntity();

            descriptor.Field(x => x.Id).Resolve(x => x.Parent<Setting>().Id ?? "").Type<NonNullType<StringType>>();
            //descriptor.Field(x => x.ValidScopes);
            //descriptor.Field(x => x.ScopedKey);
            //descriptor.Field(x => x.Name);
            //descriptor.Field(x => x.Value);
        }
    }
}
