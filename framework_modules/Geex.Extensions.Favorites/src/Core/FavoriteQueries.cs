using Geex.Extensions.Authentication;
using Geex.MultiTenant;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;

namespace Geex.Extensions.Favorites.Core;

public static class FavoriteQueries
{
    internal static string RequireUserId(this IUnitOfWork work)
    {
        var userId = work.GetCurrentUser()?.UserId;
        if (string.IsNullOrEmpty(userId))
            throw new BusinessException(GeexExceptionType.ValidationFailed, message: "需要登录后使用收藏.");
        return userId;
    }

    internal static string NormalizeId(string id) => string.IsNullOrWhiteSpace(id)
        ? throw new BusinessException(GeexExceptionType.ValidationFailed, message: "EntityId 不能为空.")
        : ObjectId.TryParse(id, out var objectId) ? objectId.ToString() : id;

    internal static IQueryable<Favorite> FavoritesFor(this IUnitOfWork work, FavoriteEntityType type)
    {
        var userId = work.RequireUserId();
        var tenantCode = work.ServiceProvider.GetService<ICurrentTenant>()?.Code ?? "";
        return work.Query<Favorite>().Where(item => item.UserId == userId && item.EntityType == type &&
            (item.TenantCode == tenantCode || (tenantCode == "" && item.TenantCode == null)));
    }

    public static async Task<IQueryable<FavoriteItem>> Favorites(this IUnitOfWork work, FavoriteEntityType type, CancellationToken token = default, bool defaultOrder = true)
    {
        var records = work.FavoritesFor(type);
        var config = (await FavoritesSettings.EntityTypes.GetEffectiveTypes(work, token)).SingleOrDefault(item => item.EntityType == type);
        if (config == null) return Array.Empty<FavoriteItem>().AsQueryable();
        var route = config.DetailRoute;
        var items = records.AsNoTracking().Select(item => new FavoriteItem
            {
                Id = item.Id, EntityType = item.EntityType, EntityId = item.EntityId, CreatedOn = item.CreatedOn,
                DisplayValue = item.Title ?? item.EntityId, DetailRoute = route
            });
        return defaultOrder ? items.OrderByDescending(item => item.CreatedOn).ThenByDescending(item => item.Id) : items;
    }

    public static async Task<IReadOnlyList<FavoriteStatus>> FavoriteStatuses(this IUnitOfWork work, FavoriteEntityType type,
        string[] entityIds, CancellationToken token = default)
    {
        var records = work.FavoritesFor(type);
        if (entityIds.Length > 500) throw new BusinessException(GeexExceptionType.ValidationFailed, message: "一次最多查询 500 个收藏状态.");
        var ids = entityIds.Select(NormalizeId).Distinct(StringComparer.Ordinal).ToArray();
        var enabled = (await FavoritesSettings.EntityTypes.GetEffectiveTypes(work, token)).Any(item => item.EntityType == type);
        var saved = enabled ? records.Where(item => ids.Contains(item.EntityId))
            .Select(item => new { item.EntityId, item.CreatedOn }).ToDictionary(item => item.EntityId, item => item.CreatedOn) : [];
        return ids.Select(id => saved.TryGetValue(id, out var date)
            ? new FavoriteStatus { EntityType = type, EntityId = id, IsFavorite = true, CreatedOn = date.ToUniversalTime() }
            : new FavoriteStatus { EntityType = type, EntityId = id }).ToArray();
    }
}
