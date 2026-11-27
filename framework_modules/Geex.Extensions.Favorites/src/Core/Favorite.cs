using Geex.MultiTenant;
using Geex.Storage;

namespace Geex.Extensions.Favorites.Core;

public partial class Favorite : Entity<Favorite>, ITenantFilteredEntity
{
    private string _tenantCode = "";
    private Favorite() { }

    private Favorite(FavoriteEntityType type, string entityId, string title, IUnitOfWork work)
    {
        UserId = work.RequireUserId();
        EntityType = type;
        EntityId = entityId;
        Title = title;
        work.Attach(this);
        // MongoDB stores milliseconds. The first response and a subsequent read must agree.
        CreatedOn = DateTimeOffset.FromUnixTimeMilliseconds(CreatedOn.ToUnixTimeMilliseconds());
    }

    public string? TenantCode { get => _tenantCode; set => _tenantCode = value ?? ""; }
    public string UserId { get; private set; } = "";
    public FavoriteEntityType EntityType { get; private set; } = null!;
    public string EntityId { get; private set; } = "";
    public string? Title { get; private set; }

    public static async Task<FavoriteStatus> Add(IUnitOfWork work, FavoriteReference reference, CancellationToken token = default)
    {
        var type = reference.EntityType;
        var records = work.FavoritesFor(type);
        var id = FavoriteQueries.NormalizeId(reference.EntityId);
        var config = (await FavoritesSettings.EntityTypes.GetEffectiveTypes(work, token)).SingleOrDefault(item => item.EntityType == type)
            ?? throw new BusinessException(GeexExceptionType.ValidationFailed, message: "当前实体类型未启用收藏.");
        var record = records.SingleOrDefault(item => item.EntityId == id);
        if (record == null)
        {
            var title = await type.ReadTitle(work, config, id, token);
            record = new Favorite(type, id, title, work);
        }
        return new() { EntityType = type, EntityId = id, IsFavorite = true, CreatedOn = record.CreatedOn.ToUniversalTime() };
    }

    public static async Task<bool> Remove(IUnitOfWork work, FavoriteReference reference, CancellationToken token = default)
    {
        var id = FavoriteQueries.NormalizeId(reference.EntityId);
        var record = work.FavoritesFor(reference.EntityType).SingleOrDefault(item => item.EntityId == id);
        if (record != null)
            await work.DbContext.RemoveOnSave(record, token);
        return true;
    }
}
