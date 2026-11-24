using Geex.Extensions.Favorites.Core;
using Geex.Extensions.Settings;
using Geex.Notifications;
using MediatX;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Entities;
using Volo.Abp.Modularity;

namespace Geex.Extensions.Favorites;

[DependsOn(typeof(SettingsModule))]
public class FavoritesModule : GeexModule<FavoritesModule, FavoritesModuleOptions>
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddScoped<FavoriteSettingsValidator>();
        context.Services.AddScoped<ISettingValueValidator>(provider => provider.GetRequiredService<FavoriteSettingsValidator>());
        context.Services.AddScoped<FavoriteService>();
        base.ConfigureServices(context);
    }

    public override void PostConfigureServices(ServiceConfigurationContext context)
    {
        var types = FavoriteEntityType.List.SelectMany(item => item.EntityType.GetInterfaces().Append(item.EntityType))
            .Where(type => typeof(IEntityBase).IsAssignableFrom(type)).Distinct();
        foreach (var type in types)
            context.Services.AddScoped(typeof(IEventHandler<>).MakeGenericType(typeof(EntityDeletedEvent<>).MakeGenericType(type)),
                typeof(FavoriteDeletionHandler<>).MakeGenericType(type));
        base.PostConfigureServices(context);
    }
}

public class FavoritesModuleOptions : GeexModuleOptions<FavoritesModule> { }
