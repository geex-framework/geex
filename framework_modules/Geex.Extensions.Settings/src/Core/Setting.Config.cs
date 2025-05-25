using Geex.Abstractions;
using HotChocolate.Types;
using MongoDB.Bson.Serialization;

namespace Geex.Extensions.Settings.Core;

public partial class Setting
{
    public class SettingBsonConfig : BsonConfig<Setting>
    {
        protected override void Map(BsonClassMap<Setting> map, BsonIndexConfig<Setting> indexConfig)
        {
            map.Inherit<ISetting>();
            map.AutoMap();
            indexConfig.MapEntityDefaultIndex();
            indexConfig.MapIndex(x => x.Hashed(y => y.Scope), options => options.Background = true);
            indexConfig.MapIndex(x => x.Hashed(y => y.Name), options => options.Background = true);
            indexConfig.MapIndex(x => x.Hashed(y => y.ScopedKey), options =>
            {
                options.Background = true;
                options.Sparse = true;
            });
        }
    }

    public class SettingGqlConfig : GqlConfig.Object<Setting>
    {
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
