using Geex.Extensions.BackgroundJob;
using Geex.Extensions.BlobStorage;
using Geex.Extensions.Backups.Core.Jobs;
using Geex.Extensions.Backups.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.Modularity;

namespace Geex.Extensions.Backups;

[DependsOn(typeof(BackgroundJobModule), typeof(BlobStorageModule), typeof(Geex.Extensions.Messaging.MessagingModule))]
public class BackupsModule : GeexModule<BackupsModule, BackupsModuleOptions>
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        base.ConfigureServices(context);
        SchemaBuilder.TryAddGeexAssembly(typeof(BackupsModule).Assembly);
        RegisterJobs(context.Services, ModuleOptions);
    }

    internal static void RegisterJobs(IServiceCollection services, BackupsModuleOptions options)
    {
        services.TryAddSingleton<IBackupArchiveExporter, MongodumpProcess>();
        // Execution has a shutdown lifecycle even when no Cron job is registered.
        services.AddSingleton<BackupsExecution>();
        services.AddHostedService(sp => sp.GetRequiredService<BackupsExecution>());
        if (!options.Enabled || services.GetSingletonInstance<BackgroundJobModuleOptions>().Disabled)
            return;
        options.ValidateAutomatic(services.GetSingletonInstance<GeexCoreModuleOptions>());
        services.AddJob<BackupsJob>();
    }
}
