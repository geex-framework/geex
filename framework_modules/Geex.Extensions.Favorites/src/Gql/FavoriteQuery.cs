using Geex.Extensions.Favorites.Core;
using Geex.Gql.Types;
using HotChocolate.Types;

namespace Geex.Extensions.Favorites.Gql;

public sealed class FavoriteQuery(FavoriteService favorites) : QueryExtension<FavoriteQuery>
{
    protected override void Configure(IObjectTypeDescriptor<FavoriteQuery> descriptor)
    {
        descriptor.Field(query => query.FavoriteEntityTypes()).Authorize();
        descriptor.Field(query => query.FavoriteStatuses(default!, default!)).Authorize();
        descriptor.Field(query => query.Favorites(default!)).Authorize()
            .UseOffsetPaging<ObjectType<FavoriteItem>>().UseFiltering<FavoriteItem>().UseSorting<FavoriteItem>();
        base.Configure(descriptor);
    }

    public Task<IReadOnlyList<FavoriteTypeConfiguration>> FavoriteEntityTypes() => favorites.GetTypes();
    public Task<IReadOnlyList<FavoriteStatus>> FavoriteStatuses(FavoriteEntityType entityType, string[] entityIds)
        => favorites.Statuses(entityType, entityIds);
    public Task<IQueryable<FavoriteItem>> Favorites(FavoriteEntityType entityType) => favorites.Query(entityType);
}
