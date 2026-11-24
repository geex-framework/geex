using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Geex.Extensions.Settings;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Types;

namespace Geex.Extensions.Favorites.Core;

public sealed class FavoriteSettingsValidator(IRequestExecutorResolver executors) : ISettingValueValidator
{
    public async Task ValidateAsync(SettingDefinition definition, JsonNode? value, CancellationToken cancellationToken = default)
    {
        if (definition.Value != FavoritesSettings.EntityTypes.Value) return;
        await ParseAsync(value, cancellationToken);
    }

    public async Task<IReadOnlyList<FavoriteTypeConfiguration>> ParseAsync(JsonNode? value, CancellationToken cancellationToken = default)
    {
        if (value is not JsonArray array) throw Invalid("Favorites_EntityTypes 必须为 JSON 数组.");
        if (array.Count == 0) return [];
        var schema = (await executors.GetRequestExecutorAsync(cancellationToken: cancellationToken)).Schema;
        var result = new List<FavoriteTypeConfiguration>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in array)
        {
            if (node is not JsonObject entry || entry.Any(pair => pair.Key is not ("EntityType" or "NameField" or "DetailRoute")))
                throw Invalid("收藏类型配置仅支持 EntityType / NameField / DetailRoute.");
            var name = Text(entry, "EntityType");
            var type = FavoriteEntityType.List.SingleOrDefault(item => item.Value == name)
                ?? throw Invalid($"未注册的收藏实体类型: {name}.");
            if (!seen.Add(type.Value)) throw Invalid($"重复的收藏实体类型: {type.Value}.");
            var fieldName = Text(entry, "NameField");
            var property = type.EntityType.GetProperty(fieldName, BindingFlags.Instance | BindingFlags.Public);
            var objectType = schema.Types.OfType<ObjectType>().FirstOrDefault(item => item.RuntimeType == type.EntityType);
            var field = objectType?.Fields.FirstOrDefault(item => item.ResolveEntityProperty(type.EntityType)?.Name == fieldName);
            if (property?.PropertyType != typeof(string) || property.GetMethod?.IsPublic != true ||
                property.GetIndexParameters().Length != 0 || field == null || field.Directives.Any(directive => directive.Type.Name == "authorize"))
                throw Invalid($"{type.Value}.{fieldName} 必须是 GraphQL 公开可读的顶层字符串属性.");
            var route = entry["DetailRoute"] == null ? null : Text(entry, "DetailRoute");
            if (!string.IsNullOrEmpty(route) && (!Regex.IsMatch(route, @"^/(?!/)[A-Za-z0-9_/{}/-]*$") ||
                route.Replace("{id}", "", StringComparison.Ordinal).IndexOfAny(['{', '}']) >= 0))
                throw Invalid("DetailRoute 必须为应用内路径, 仅支持 {id} 占位符.");
            result.Add(new FavoriteTypeConfiguration { EntityType = type, NameField = fieldName, DetailRoute = route });
        }
        return result;
    }

    private static string Text(JsonObject node, string name)
        => node[name] is JsonValue value && value.TryGetValue<string>(out var text)
            ? text : throw Invalid($"{name} 必须为字符串.");

    private static BusinessException Invalid(string message) => new(GeexExceptionType.ValidationFailed, message: message);
}
