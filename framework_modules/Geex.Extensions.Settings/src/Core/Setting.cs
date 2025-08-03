using System;
using Geex.Validation;
using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Geex.Storage;

namespace Geex.Extensions.Settings.Core;

[DebuggerDisplay("{Name}")]
public partial class Setting : Entity<Setting>, ISetting
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

    public override async Task<ValidationResult> Validate(CancellationToken cancellation = default)
    {
        return ValidationResult.Success;
    }
}
