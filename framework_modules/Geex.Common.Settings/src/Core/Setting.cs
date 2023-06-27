using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

using Geex.Common.Abstraction;
using Geex.Common.Abstraction.MultiTenant;
using Geex.Common.Abstraction.Storage;
using Geex.Common.Settings.Abstraction;
using Geex.Common.Settings.Api.Aggregates.Settings;

using HotChocolate.Types;

using MongoDB.Bson.Serialization;

namespace Geex.Common.Settings.Core;

[DebuggerDisplay("{Name}")]
public class Setting : Entity<Setting>, ISetting
{
    public Setting(SettingDefinition name, JsonNode value, SettingScopeEnumeration scope, string? scopedKey = default)
    {
        if (value is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var strValue) &&
            (strValue.StartsWith("{") || strValue.StartsWith("[")))
        {
            throw new InvalidOperationException("尝试存入字符串json?");
        }

        Name = name;
        Value = value;
        Scope = scope;
        ScopedKey = scopedKey;
    }

    public SettingDefinition Name { get; private set; }
    public SettingScopeEnumeration[] ValidScopes => Name.ValidScopes;
    public SettingScopeEnumeration Scope { get; private set; }
    public string? ScopedKey { get; private set; }
    public JsonNode? Value { get; private set; }

    public void SetValue(JsonNode? value)
    {
        Value = value;
    }

    public override async Task<ValidationResult> Validate(IServiceProvider sp, CancellationToken cancellation = default)
    {
        return ValidationResult.Success;
    }

    public class SettingBsonConfig : BsonConfig<Setting>
    {
        protected override void Map(BsonClassMap<Setting> map)
        {
            map.Inherit<ISetting>();
            map.AutoMap();
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
