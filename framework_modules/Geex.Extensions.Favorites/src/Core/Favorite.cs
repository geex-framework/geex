using Geex.MultiTenant;
using Geex.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Geex.Extensions.Favorites.Core;

public partial class Favorite : Entity<Favorite>, ITenantFilteredEntity
{
    private string _tenantCode = "";
    private Favorite() { }

    private Favorite(string userId, FavoriteEntityType type, string entityId, string title)
        => (UserId, EntityType, EntityId, Title) = (userId, type, entityId, title);

    public string? TenantCode { get => _tenantCode; set => _tenantCode = value ?? ""; }
    public string UserId { get; private set; } = "";
    public FavoriteEntityType EntityType { get; private set; } = null!;
    public string EntityId { get; private set; } = "";
    public string? Title { get; private set; }

    public static async Task<FavoriteStatus> Add(IUnitOfWork work, FavoriteReference reference, CancellationToken token = default)
    {
        var owner = FavoriteOwner.Current(work);
        var type = reference.EntityType;
        var id = FavoriteQueries.NormalizeId(reference.EntityId);
        var config = (await FavoritesSettings.EntityTypes.GetEffectiveTypes(work, token)).SingleOrDefault(item => item.EntityType == type)
            ?? throw new BusinessException(GeexExceptionType.ValidationFailed, message: "当前实体类型未启用收藏.");
        var writes = work.ServiceProvider.GetRequiredService<FavoriteWriteScope>();
        await writes.Enter(owner, token);
        var record = writes.Find(owner, type, id);
        if (record == null)
        {
            var title = await type.ReadTitle(work, config, id, token);
            record = work.Attach(new Favorite(owner.User, type, id, title));
            record.CreatedOn = DateTimeOffset.FromUnixTimeMilliseconds(record.CreatedOn.ToUnixTimeMilliseconds());
            writes.Remember(owner, type, id, record);
        }
        return new() { EntityType = type, EntityId = id, IsFavorite = true, CreatedOn = record.CreatedOn.ToUniversalTime() };
    }

    public static async Task<bool> Remove(IUnitOfWork work, FavoriteReference reference, CancellationToken token = default)
    {
        var owner = FavoriteOwner.Current(work);
        var id = FavoriteQueries.NormalizeId(reference.EntityId);
        var type = reference.EntityType;
        var writes = work.ServiceProvider.GetRequiredService<FavoriteWriteScope>();
        await writes.Enter(owner, token);
        if (writes.Find(owner, type, id) is { } record)
        {
            await work.DbContext.RemoveOnSave(record, token);
            writes.Remember(owner, type, id, null);
        }
        return true;
    }
}
