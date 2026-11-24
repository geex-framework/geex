using Geex.Extensions.Authentication;
using Geex.Extensions.Settings;
using Geex.MultiTenant;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Entities;

namespace Geex.Extensions.Favorites.Core;

public sealed class FavoriteService(IUnitOfWork work, ICurrentUser currentUser,
    ISettingService settings, FavoriteSettingsValidator validator)
{
    private string UserId => !string.IsNullOrEmpty(currentUser.UserId) ? currentUser.UserId
        : throw Invalid("需要登录后使用收藏.");
    private string TenantCode => NormalizeTenant(work.ServiceProvider.GetService<ICurrentTenant>()?.Code);

    public async Task<IReadOnlyList<FavoriteTypeConfiguration>> GetTypes(CancellationToken cancellationToken = default)
    {
        _ = UserId;
        var setting = await settings.GetSetting(FavoritesSettings.EntityTypes);
        return await validator.ParseAsync(setting?.Value ?? FavoritesSettings.EntityTypes.DefaultValue, cancellationToken);
    }

    private IQueryable<Favorite> OwnFavorites(FavoriteEntityType type)
    {
        var userId = UserId;
        var tenantCode = TenantCode;
        return work.Query<Favorite>().Where(item => item.UserId == userId && item.TenantCode == tenantCode && item.EntityType == type);
    }

    private Dictionary<string, IEntityBase> VisibleEntities(FavoriteEntityType type, IEnumerable<string> entityIds)
    {
        var tenantCode = TenantCode;
        var result = new Dictionary<string, IEntityBase>(StringComparer.Ordinal);
        foreach (var batch in entityIds.Distinct(StringComparer.Ordinal).Chunk(200))
        {
            foreach (var entity in type.Query(work, batch).ToList())
            {
                if (entity is ITenantFilteredEntity scoped && NormalizeTenant(scoped.TenantCode) != tenantCode) continue;
                result[entity.Id] = entity;
            }
        }
        return result;
    }

    public async Task<IQueryable<FavoriteItem>> Query(FavoriteEntityType type, CancellationToken cancellationToken = default)
    {
        var configuration = (await GetTypes(cancellationToken)).SingleOrDefault(item => item.EntityType == type);
        if (configuration == null) return Array.Empty<FavoriteItem>().AsQueryable();
        var favorites = OwnFavorites(type).OrderByDescending(item => item.CreatedOn).ThenByDescending(item => item.Id).ToList();
        var entities = VisibleEntities(type, favorites.Select(item => item.EntityId));
        var property = type.EntityType.GetProperty(configuration.NameField)!;
        return favorites.Where(item => entities.ContainsKey(item.EntityId)).Select(item =>
        {
            var value = property.GetValue(entities[item.EntityId]) as string;
            return new FavoriteItem
            {
                Id = item.Id, EntityId = item.EntityId, EntityType = type, CreatedOn = item.CreatedOn,
                DisplayValue = string.IsNullOrWhiteSpace(value) ? item.EntityId : value,
                DetailUrl = string.IsNullOrEmpty(configuration.DetailRoute) ? null
                    : configuration.DetailRoute.Replace("{id}", Uri.EscapeDataString(item.EntityId), StringComparison.Ordinal),
            };
        }).AsQueryable();
    }

    public async Task<IReadOnlyList<FavoriteStatus>> Statuses(FavoriteEntityType type, string[] entityIds,
        CancellationToken cancellationToken = default)
    {
        if (entityIds.Length > 500) throw Invalid("一次最多查询 500 个收藏状态.");
        var ids = entityIds.Select(NormalizeId).Distinct(StringComparer.Ordinal).ToArray();
        var enabled = (await GetTypes(cancellationToken)).Any(item => item.EntityType == type);
        var visible = enabled ? VisibleEntities(type, ids) : [];
        var favorites = enabled ? OwnFavorites(type).Where(item => ids.Contains(item.EntityId)).ToList()
            .ToDictionary(item => item.EntityId, StringComparer.Ordinal) : new Dictionary<string, Favorite>();
        return ids.Select(id =>
        {
            favorites.TryGetValue(id, out var record);
            var isFavorite = record != null && visible.ContainsKey(id);
            return new FavoriteStatus { EntityType = type, EntityId = id, IsFavorite = isFavorite, CreatedOn = isFavorite ? record!.CreatedOn : null };
        }).ToArray();
    }

    public async Task<FavoriteStatus> Add(FavoriteReference reference, CancellationToken cancellationToken = default)
    {
        var id = NormalizeId(reference.EntityId);
        var type = reference.EntityType;
        if (!(await GetTypes(cancellationToken)).Any(item => item.EntityType == type))
            throw Invalid("当前实体类型未启用收藏.");
        if (!VisibleEntities(type, [id]).ContainsKey(id))
            throw new BusinessException(GeexExceptionType.NotFound, message: "对象不存在或无权访问.");
        var userId = UserId;
        var tenantCode = TenantCode;
        var collection = DB.Collection<Favorite>();
        var filter = Builders<Favorite>.Filter.Where(item => item.TenantCode == tenantCode && item.UserId == userId &&
            item.EntityType == type && item.EntityId == id);
        var now = DateTimeOffset.UtcNow;
        var update = Builders<Favorite>.Update
            .SetOnInsert(item => item.Id, ObjectId.GenerateNewId().ToString())
            .SetOnInsert(item => item.TenantCode, tenantCode).SetOnInsert(item => item.UserId, userId)
            .SetOnInsert(item => item.EntityType, type).SetOnInsert(item => item.EntityId, id)
            .SetOnInsert(item => item.CreatedOn, now).SetOnInsert(item => item.ModifiedOn, now);
        Favorite favorite;
        try
        {
            favorite = await collection.FindOneAndUpdateAsync(work.DbContext.Session, filter, update,
                new FindOneAndUpdateOptions<Favorite> { IsUpsert = true, ReturnDocument = ReturnDocument.After }, cancellationToken);
        }
        catch (MongoCommandException exception) when (exception.Code == 11000 && !work.IsInExplicitTransaction)
        {
            favorite = await collection.Find(work.DbContext.Session, filter).SingleAsync(cancellationToken);
        }
        return new FavoriteStatus { EntityType = type, EntityId = id, IsFavorite = true, CreatedOn = favorite.CreatedOn };
    }

    public async Task<bool> Remove(FavoriteReference reference, CancellationToken cancellationToken = default)
    {
        var userId = UserId;
        var tenantCode = TenantCode;
        var id = NormalizeId(reference.EntityId);
        await work.DeleteAsync<Favorite>(item => item.UserId == userId && item.TenantCode == tenantCode &&
            item.EntityType == reference.EntityType && item.EntityId == id, cancellationToken);
        return true;
    }

    private static string NormalizeId(string id) => string.IsNullOrWhiteSpace(id)
        ? throw Invalid("EntityId 不能为空.") : ObjectId.TryParse(id, out var objectId) ? objectId.ToString() : id;
    private static string NormalizeTenant(string? tenant) => tenant ?? "";
    private static BusinessException Invalid(string message) => new(GeexExceptionType.ValidationFailed, message: message);
}
