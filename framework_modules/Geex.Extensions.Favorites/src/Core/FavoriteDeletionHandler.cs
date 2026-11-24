using Geex.Notifications;
using MediatX;
using MongoDB.Entities;

namespace Geex.Extensions.Favorites.Core;

public sealed class FavoriteDeletionHandler<T>(IUnitOfWork work) : IEventHandler<EntityDeletedEvent<T>> where T : IEntityBase
{
    public async Task Handle(EntityDeletedEvent<T> notification, CancellationToken cancellationToken)
    {
        var types = FavoriteEntityType.List.Where(type => typeof(T).IsAssignableFrom(type.EntityType) || type.EntityType.IsAssignableFrom(typeof(T)));
        // 接口类型的事件可能来自其他集合, 清理前确认目标已真实删除, 不受当前用户的可见性影响.
        using var scope = work.DbContext.DisableAllDataFilters();
        foreach (var type in types)
        {
            if (type.Exists(work, notification.EntityId)) continue;
            await work.DeleteAsync<Favorite>(item => item.EntityType == type && item.EntityId == notification.EntityId, cancellationToken);
        }
    }
}
