using Geex.MultiTenant;
using Geex.Storage;

namespace Geex.Extensions.Favorites.Core;

public partial class Favorite : Entity<Favorite>, ITenantFilteredEntity
{
    public string? TenantCode { get; set; }
    public string UserId { get; set; } = "";
    public FavoriteEntityType EntityType { get; set; } = null!;
    public string EntityId { get; set; } = "";
}
