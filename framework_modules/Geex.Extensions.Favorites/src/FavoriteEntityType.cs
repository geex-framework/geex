using System.Reflection;
using MongoDB.Entities;

namespace Geex.Extensions.Favorites;

public class FavoriteEntityType : Enumeration<FavoriteEntityType>
{
    private readonly Func<IUnitOfWork, string[], IQueryable<IEntityBase>> _query;

    public FavoriteEntityType(string value, Type entityType, string? defaultNameField = null, string? defaultDetailRoute = null)
        : base(value)
    {
        if (!typeof(IEntityBase).IsAssignableFrom(entityType))
            throw new ArgumentException("Favorite types must implement IEntityBase.", nameof(entityType));
        EntityType = entityType;
        DefaultNameField = defaultNameField;
        DefaultDetailRoute = defaultDetailRoute;
        _query = (Func<IUnitOfWork, string[], IQueryable<IEntityBase>>)typeof(FavoriteEntityType)
            .GetMethod(nameof(QueryEntities), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(entityType).CreateDelegate(typeof(Func<IUnitOfWork, string[], IQueryable<IEntityBase>>));
    }

    public Type EntityType { get; }
    public string? DefaultNameField { get; }
    public string? DefaultDetailRoute { get; }

    public virtual IQueryable<IEntityBase> Query(IUnitOfWork work, string[] ids) => _query(work, ids);

    internal bool Exists(IUnitOfWork work, string id) => _query(work, [id]).Any();

    private static IQueryable<IEntityBase> QueryEntities<T>(IUnitOfWork work, string[] ids) where T : class, IEntityBase
        => work.Query<T>().Where(entity => ids.Contains(entity.Id));
}
