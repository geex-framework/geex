using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Geex.Extensions.AuditLogs.Core.Entities;
using Geex.Extensions.BackgroundJob.Core;
using Geex.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Geex.Extensions.AuditLogs.Core.Jobs;

public class AuditLogRetentionJob : CronJob<AuditLogRetentionJob>
{
    public AuditLogRetentionJob(IServiceProvider sp, string cronExp) : base(sp, cronExp)
    {
    }

    public override bool IsConcurrentAllowed => false;

    public override async Task Run(IServiceProvider serviceProvider, CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var options = scope.ServiceProvider.GetRequiredService<AuditLogsModuleOptions>();
        using var _ = uow.As<GeexDbContext>().DisableAllDataFilters();
        var cutoff = DateTimeOffset.UtcNow.AddDays(-options.RetentionDays);
        var expired = uow.Query<AuditLog>().Where(x => x.CreatedOn < cutoff).ToList();
        if (expired.Count == 0)
            return;
        await uow.DeleteAsync<AuditLog>(x => x.CreatedOn < cutoff, stoppingToken);
        await uow.SaveChanges(stoppingToken);
    }
}
