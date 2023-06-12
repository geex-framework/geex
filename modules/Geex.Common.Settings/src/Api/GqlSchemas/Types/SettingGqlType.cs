using Geex.Common.Settings.Api.Aggregates.Settings;
using Geex.Common.Settings.Core;
using HotChocolate.Types;

namespace Geex.Common.Settings.Api.GqlSchemas.Types
{
    public class SettingGqlType : ObjectType<Setting>
    {
        protected override void Configure(IObjectTypeDescriptor<Setting> descriptor)
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Implements<InterfaceType<ISetting>>();
            descriptor.ConfigEntity();
            descriptor.Field(x => x.Scope);
            descriptor.Field(x => x.ValidScopes);
            descriptor.Field(x => x.ScopedKey);
            descriptor.Field(x => x.Name);
            descriptor.Field(x => x.Id);
            descriptor.Field(x => x.Value);
            base.Configure(descriptor);
        }
    }
}
