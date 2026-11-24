using System.Text.Json.Nodes;
using Geex.Extensions.Settings;

namespace Geex.Extensions.Favorites;

public class FavoritesSettings : SettingDefinition
{
    private FavoritesSettings() : base("Favorites_EntityTypes",
        [SettingScopeEnumeration.Global, SettingScopeEnumeration.Tenant],
        "个人收藏实体类型/显示字段/详情路径", defaultValue: CreateDefaults()) { }

    public static FavoritesSettings EntityTypes { get; } = new();

    private static JsonArray CreateDefaults() => new(FavoriteEntityType.List
        .Where(type => type.DefaultNameField != null)
        .Select(type => (JsonNode)new JsonObject
        {
            [nameof(FavoriteTypeConfiguration.EntityType)] = type.Value,
            [nameof(FavoriteTypeConfiguration.NameField)] = type.DefaultNameField,
            [nameof(FavoriteTypeConfiguration.DetailRoute)] = type.DefaultDetailRoute,
        }).ToArray());
}
