using Geex.Extensions.Favorites.Gql;
using Geex.Extensions.Settings;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace Geex.Extensions.Favorites;

[DependsOn(typeof(SettingsModule))]
public class FavoritesModule : GeexModule<FavoritesModule, FavoritesModuleOptions>
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        base.ConfigureServices(context);
        SchemaBuilder.TryAddTypeInterceptor<FavoriteTitleFieldInterceptor>();
    }
}

public class FavoritesModuleOptions : GeexModuleOptions<FavoritesModule> { }
