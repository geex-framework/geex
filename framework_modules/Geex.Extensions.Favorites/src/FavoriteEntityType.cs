using System.Reflection;
using MongoDB.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Geex.Extensions.Favorites;

public class FavoriteEntityType : Enumeration<FavoriteEntityType>
{
    private readonly Func<IUnitOfWork, string[], IQueryable<IEntityBase>> _query;
    private readonly Dictionary<string, Func<IUnitOfWork, IEntityBase, CancellationToken, Task<string?>>> _titleReaders = new(StringComparer.Ordinal);

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

    protected void RegisterTitleField(string fieldName, Func<IUnitOfWork, IEntityBase, CancellationToken, Task<string?>> readAuthorizedTitle)
        => _titleReaders.Add(fieldName, readAuthorizedTitle);

    internal bool HasTitleReader(string name) => _titleReaders.ContainsKey(name);

    internal async Task<string> ReadTitle(IUnitOfWork work, FavoriteTypeConfiguration config, string id, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        Exception? reason = null;
        try
        {
            var entity = Query(work, [id]).SingleOrDefault();
            if (entity != null)
            {
                if (!_titleReaders.TryGetValue(config.NameField, out var read))
                {
                    var value = (string?)config.TitleProperty!.GetValue(entity);
                    return string.IsNullOrWhiteSpace(value) ? id : value;
                }
                var title = await read(work, entity, token);
                if (title != null) return string.IsNullOrWhiteSpace(title) ? id : title;
            }
        }
        catch (Exception error) when (error is UnauthorizedAccessException ||
            error is BusinessException business && business.ExceptionCode == GeexExceptionType.NotFound.Value)
        {
            reason = error;
        }
        work.ServiceProvider.GetRequiredService<ILogger<FavoriteEntityType>>()
            .LogError(reason, "Favorite title unavailable for {EntityType} {EntityId}, field {NameField}.", Value, id, config.NameField);
        return id;
    }

    private static IQueryable<IEntityBase> QueryEntities<T>(IUnitOfWork work, string[] ids) where T : class, IEntityBase
        => work.Query<T>().Where(entity => ids.Contains(entity.Id));
}
