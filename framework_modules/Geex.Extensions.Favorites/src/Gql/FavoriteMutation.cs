using Geex.Extensions.Favorites.Core;
using Geex.Gql.Types;
using HotChocolate.Types;

namespace Geex.Extensions.Favorites.Gql;

public sealed class FavoriteMutation(FavoriteService favorites) : MutationExtension<FavoriteMutation>
{
    protected override void Configure(IObjectTypeDescriptor<FavoriteMutation> descriptor)
    {
        descriptor.Field(mutation => mutation.AddFavorite(default!)).Authorize();
        descriptor.Field(mutation => mutation.RemoveFavorite(default!)).Authorize();
        base.Configure(descriptor);
    }

    public Task<FavoriteStatus> AddFavorite(FavoriteReference request) => favorites.Add(request);
    public Task<bool> RemoveFavorite(FavoriteReference request) => favorites.Remove(request);
}
