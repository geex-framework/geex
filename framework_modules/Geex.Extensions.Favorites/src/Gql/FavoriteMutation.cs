using Geex.Extensions.Favorites.Core;
using Geex.Gql.Types;
using HotChocolate.Types;

namespace Geex.Extensions.Favorites.Gql;

public sealed class FavoriteMutation(IUnitOfWork work) : MutationExtension<FavoriteMutation>
{
    protected override void Configure(IObjectTypeDescriptor<FavoriteMutation> descriptor)
    {
        descriptor.Field(mutation => mutation.AddFavorite(default!, default)).Authorize();
        descriptor.Field(mutation => mutation.RemoveFavorite(default!, default)).Authorize();
        base.Configure(descriptor);
    }

    public Task<FavoriteStatus> AddFavorite(FavoriteReference request, CancellationToken token) => Favorite.Add(work, request, token);
    public Task<bool> RemoveFavorite(FavoriteReference request, CancellationToken token) => Favorite.Remove(work, request, token);
}
