using Geex.Extensions.Authentication;
using Geex.MultiTenant;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;

namespace Geex.Extensions.Favorites.Core;

internal readonly record struct FavoriteOwner(string User, string Tenant)
{
    public static FavoriteOwner Current(IUnitOfWork work)
    {
        var user = work.GetCurrentUser()?.UserId;
        if (string.IsNullOrEmpty(user))
            throw new BusinessException(GeexExceptionType.ValidationFailed, message: "需要登录后使用收藏.");
        return new(user, work.ServiceProvider.GetService<ICurrentTenant>()?.Code ?? "");
    }
}

public static class FavoriteQueries
{
    internal static string NormalizeId(string id) => string.IsNullOrWhiteSpace(id)
        ? throw new BusinessException(GeexExceptionType.ValidationFailed, message: "EntityId 不能为空.")
        : ObjectId.TryParse(id, out var objectId) ? objectId.ToString() : id;

    internal static IQueryable<Favorite> FavoritesFor(this IUnitOfWork work, FavoriteOwner owner, FavoriteEntityType type)
    {
        var tenant = owner.Tenant;
        var user = owner.User;
        return work.Query<Favorite>().Where(item => item.UserId == user && item.EntityType == type &&
            (item.TenantCode == tenant || (tenant == "" && item.TenantCode == null)));
    }

    public static async Task<IQueryable<FavoriteItem>> Favorites(this IUnitOfWork work, FavoriteEntityType type, CancellationToken token = default, bool defaultOrder = true)
    {
        var owner = FavoriteOwner.Current(work);
        var config = (await FavoritesSettings.EntityTypes.GetEffectiveTypes(work, token)).SingleOrDefault(item => item.EntityType == type);
        if (config == null) return Array.Empty<FavoriteItem>().AsQueryable();
        var route = config.DetailRoute;
        var items = work.FavoritesFor(owner, type).AsNoTracking().Select(item => new FavoriteItem
            {
                Id = item.Id, EntityType = item.EntityType, EntityId = item.EntityId, CreatedOn = item.CreatedOn,
                DisplayValue = item.Title ?? item.EntityId, DetailRoute = route
            });
        return defaultOrder ? items.OrderByDescending(item => item.CreatedOn).ThenByDescending(item => item.Id) : items;
    }

    public static async Task<IReadOnlyList<FavoriteStatus>> FavoriteStatuses(this IUnitOfWork work, FavoriteEntityType type,
        string[] entityIds, CancellationToken token = default)
    {
        var owner = FavoriteOwner.Current(work);
        if (entityIds.Length > 500) throw new BusinessException(GeexExceptionType.ValidationFailed, message: "一次最多查询 500 个收藏状态.");
        var ids = entityIds.Select(NormalizeId).Distinct(StringComparer.Ordinal).ToArray();
        var enabled = (await FavoritesSettings.EntityTypes.GetEffectiveTypes(work, token)).Any(item => item.EntityType == type);
        var saved = enabled ? work.FavoritesFor(owner, type).Where(item => ids.Contains(item.EntityId))
            .Select(item => new { item.EntityId, item.CreatedOn }).ToDictionary(item => item.EntityId, item => item.CreatedOn) : [];
        return ids.Select(id => saved.TryGetValue(id, out var date)
            ? new FavoriteStatus { EntityType = type, EntityId = id, IsFavorite = true, CreatedOn = date.ToUniversalTime() }
            : new FavoriteStatus { EntityType = type, EntityId = id }).ToArray();
    }
}
