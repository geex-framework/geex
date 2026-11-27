using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Geex.Extensions.Favorites.Gql;
using Geex.Extensions.Settings;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Geex.Extensions.Favorites;

public sealed class FavoritesSettings : SettingDefinition
{
    private readonly ConditionalWeakTable<ISchema, Dictionary<(Type, string), PropertyInfo>> _fields = new();

    private FavoritesSettings() : base("Favorites_EntityTypes", [SettingScopeEnumeration.Global, SettingScopeEnumeration.Tenant],
        "个人收藏实体类型/显示字段/详情路径", defaultValue: CreateDefaults()) { }

    public static FavoritesSettings EntityTypes { get; } = new();

    public override async Task ValidateAsync(JsonNode? value, IServiceProvider services, CancellationToken cancellationToken = default)
        => await Parse(value, services, true, cancellationToken);

    public async Task<IReadOnlyList<FavoriteTypeConfiguration>> GetEffectiveTypes(IUnitOfWork work, CancellationToken token = default)
    {
        var setting = await work.GetSettingService()!.GetSetting(this);
        return await Parse(setting.Value, work.ServiceProvider, false, token);
    }

    private async Task<IReadOnlyList<FavoriteTypeConfiguration>> Parse(JsonNode? value, IServiceProvider services, bool strict, CancellationToken token)
    {
        if (value is not JsonArray array) throw Invalid("Favorites_EntityTypes 必须为 JSON 数组.");
        if (array.Count == 0) return [];
        var schema = (await services.GetRequiredService<IRequestExecutorResolver>().GetRequestExecutorAsync(cancellationToken: token)).Schema;
        var fields = _fields.GetValue(schema, ReadFields);
        var types = FavoriteEntityType.List.ToDictionary(type => type.Value, StringComparer.Ordinal);
        var result = new List<FavoriteTypeConfiguration>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in array)
        {
            try
            {
                if (node is not JsonObject entry || entry.Any(pair => pair.Key is not ("EntityType" or "NameField" or "DetailRoute")))
                    throw Invalid("收藏类型配置仅支持 EntityType / NameField / DetailRoute.");
                var name = Text(entry, "EntityType");
                if (!types.TryGetValue(name, out var type)) throw Invalid($"未注册的收藏实体类型: {name}.");
                var field = Text(entry, "NameField");
                if (!fields.TryGetValue((type.EntityType, field), out var property) && !type.HasTitleReader(field))
                    throw Invalid($"{name}.{field} 需要是公开的直接映射字符串属性或已注册的标题字段.");
                var route = entry["DetailRoute"] == null ? null : Text(entry, "DetailRoute");
                if (!string.IsNullOrEmpty(route) && (!Regex.IsMatch(route, @"^/(?!/)[A-Za-z0-9_/{}/-]*$") ||
                    route.Replace("{id}", "", StringComparison.Ordinal).IndexOfAny(['{', '}']) >= 0))
                    throw Invalid("DetailRoute 必须为应用内路径, 仅支持 {id} 占位符.");
                if (!seen.Add(name)) throw Invalid($"重复的收藏实体类型: {name}.");
                result.Add(new() { EntityType = type, NameField = field, DetailRoute = string.IsNullOrEmpty(route) ? null : route, TitleProperty = property });
            }
            catch (BusinessException error) when (!strict)
            {
                services.GetRequiredService<ILogger<FavoritesSettings>>().LogWarning(error, "Ignoring invalid favorite type configuration: {Entry}", node?.ToJsonString());
            }
        }
        return result;
    }

    private static Dictionary<(Type, string), PropertyInfo> ReadFields(ISchema schema)
    {
        var result = new Dictionary<(Type, string), PropertyInfo>();
        foreach (var type in schema.Types.OfType<ObjectType>())
        {
            foreach (var field in type.Fields)
            {
                if (field.ContextData.TryGetValue(FavoriteTitleFieldInterceptor.DirectProperty, out var value) &&
                    value is ObjectFieldDefinition { Member: PropertyInfo property } definition && definition.MiddlewareDefinitions.Count == 0 &&
                    definition.Resolver == null && definition.PureResolver == null &&
                    (definition.ResolverMember == null || definition.ResolverMember == property) && !field.Directives.Any(d => d.Type.Name == "authorize") &&
                    !type.Directives.Any(d => d.Type.Name == "authorize"))
                    result[(type.RuntimeType, property.Name)] = property;
            }
        }
        return result;
    }

    private static string Text(JsonObject entry, string key) =>
        entry[key] is JsonValue value && value.TryGetValue<string>(out var text) ? text : throw Invalid($"{key} 必须为字符串.");

    private static BusinessException Invalid(string message) => new(GeexExceptionType.ValidationFailed, message: message);

    private static JsonArray CreateDefaults() => new(FavoriteEntityType.List.Where(type => type.DefaultNameField != null)
        .Select(type => (JsonNode)new JsonObject
        {
            [nameof(FavoriteTypeConfiguration.EntityType)] = type.Value,
            [nameof(FavoriteTypeConfiguration.NameField)] = type.DefaultNameField,
            [nameof(FavoriteTypeConfiguration.DetailRoute)] = type.DefaultDetailRoute
        }).ToArray());
}
