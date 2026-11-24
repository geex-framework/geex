namespace Geex.Extensions.Favorites;

public sealed class FavoriteTypeConfiguration
{
    public FavoriteEntityType EntityType { get; init; } = null!;
    public string NameField { get; init; } = "";
    public string? DetailRoute { get; init; }
}

public sealed class FavoriteReference
{
    public FavoriteEntityType EntityType { get; set; } = null!;
    public string EntityId { get; set; } = "";
}

public sealed class FavoriteStatus
{
    public FavoriteEntityType EntityType { get; init; } = null!;
    public string EntityId { get; init; } = "";
    public bool IsFavorite { get; init; }
    public DateTimeOffset? CreatedOn { get; init; }
}

public sealed class FavoriteItem
{
    public string Id { get; init; } = "";
    public FavoriteEntityType EntityType { get; init; } = null!;
    public string EntityId { get; init; } = "";
    public DateTimeOffset CreatedOn { get; init; }
    public string DisplayValue { get; init; } = "";
    public string? DetailUrl { get; init; }
}
