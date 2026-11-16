using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Geex.Extensions.BackgroundJob.Core;
using Geex.Extensions.Identity.Core.Entities;
using Geex.MultiTenant;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Geex.Extensions.Identity.Core;

public sealed class UserDeactivationJob(IServiceProvider services, string cron)
    : CronJob<UserDeactivationJob>(services, cron, TimeZoneInfo.FindSystemTimeZoneById("Asia/Shanghai"))
{
    public override bool IsConcurrentAllowed => false;

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await base.StartAsync(cancellationToken);
        await DoWork(cancellationToken);
    }

    public override async Task Run(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var now = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow();
        using var filters = uow.DbContext.DisableAllDataFilters();
        var due = uow.Query<User>().Where(x => x.IsEnable && x.DeactivationAt != null && x.DeactivationAt <= now)
            .Select(x => new { x.Id, x.TenantCode }).ToList();
        foreach (var item in due)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using var userScope = serviceProvider.CreateScope();
                using var tenant = userScope.ServiceProvider.GetRequiredService<ICurrentTenant>().Change(item.TenantCode);
                await userScope.ServiceProvider.GetRequiredService<UserDeactivationService>().DeactivateDueUser(item.Id, cancellationToken);
            }
            catch (Exception error) when (!cancellationToken.IsCancellationRequested)
            {
                serviceProvider.GetRequiredService<ILogger<UserDeactivationJob>>().LogError(error,
                    "Scheduled deactivation failed for user {UserId}; the next run will retry.", item.Id);
            }
        }
    }
}

public sealed class UserDeactivationService(IUnitOfWork uow, TimeProvider clock, UserChangeHistory history)
{
    public async Task<bool> DeactivateDueUser(string id, CancellationToken cancellationToken)
    {
        if (!uow.DbContext.SupportTransaction || uow.DbContext.Session.IsInTransaction)
            throw new InvalidOperationException("Scheduled deactivation requires a fresh transactional unit of work.");
        _ = uow.StartExplicitTransaction();
        try
        {
        var user = uow.Query<User>().SingleOrDefault(x => x.Id == id);
        if (user == null || !user.IsEnable || user.DeactivationAt == null || user.DeactivationAt > clock.GetUtcNow())
        {
            await uow.CommitExplicitTransaction();
            return false;
        }
        var before = history.Capture(user);
        user.IsEnable = false;
        user.ScheduleDeactivation(null, clock.GetUtcNow());
        await user.RevokeSessionsAsync(cancellationToken);
        history.Record(user, before, "定时取消激活", true);
        await uow.SaveChanges(cancellationToken);
        await uow.CommitExplicitTransaction();
        return true;
        }
        catch
        {
            if (uow.DbContext.Session.IsInTransaction)
                await uow.DbContext.AbortTransactionAsync(CancellationToken.None);
            uow.DbContext.MemoryDataCache.TypedCacheDictionary.Clear();
            uow.DbContext.DbDataCache.TypedCacheDictionary.Clear();
            throw;
        }
    }
}
