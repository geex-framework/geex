using Geex.Extensions.Favorites.Core;
using Geex.Gql.Types;
using HotChocolate.Types;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace Geex.Extensions.Favorites.Gql;

public sealed class FavoriteQuery(IUnitOfWork work) : QueryExtension<FavoriteQuery>
{
    protected override void Configure(IObjectTypeDescriptor<FavoriteQuery> descriptor)
    {
        descriptor.Field(query => query.FavoriteEntityTypes(default)).Authorize();
        descriptor.Field(query => query.FavoriteStatuses(default!, default!, default)).Authorize();
        descriptor.Field(query => query.Favorites(default!, default!, default)).Authorize()
            .UseOffsetPaging<ObjectType<FavoriteItem>>().UseFiltering<FavoriteItem>().UseSorting<FavoriteItem>();
        base.Configure(descriptor);
    }

    public Task<IReadOnlyList<FavoriteTypeConfiguration>> FavoriteEntityTypes(CancellationToken token) => FavoritesSettings.EntityTypes.GetEffectiveTypes(work, token);
    public Task<IReadOnlyList<FavoriteStatus>> FavoriteStatuses(FavoriteEntityType entityType, string[] entityIds, CancellationToken token)
        => work.FavoriteStatuses(entityType, entityIds, token);
    public Task<IQueryable<FavoriteItem>> Favorites(FavoriteEntityType entityType, IResolverContext context, CancellationToken token)
    {
        var sort = context.ArgumentLiteral<IValueNode>("sort");
        return work.Favorites(entityType, token, defaultOrder: sort is NullValueNode or ListValueNode { Items.Count: 0 });
    }
}
