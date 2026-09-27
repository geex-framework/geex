using Geex.Extensions.BackgroundJob.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Geex.Extensions.Backups.Core.Jobs;

public class BackupsJob : CronJob<BackupsJob>
{
    public BackupsJob(IServiceProvider services, string cron)
        : base(services, cron) { }

    public override bool IsConcurrentAllowed => false;

    public override Task Run(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var scheduledAt = GetScheduledAt(DateTimeOffset.UtcNow);
        return scheduledAt.HasValue
            ? serviceProvider.GetRequiredService<BackupsExecution>().RunScheduledAsync(scheduledAt.Value, cancellationToken)
            : Task.CompletedTask;
    }

    internal DateTimeOffset? GetScheduledAt(DateTimeOffset now)
    {
        // The scheduler invokes Run just after an occurrence; never replay an older missed slot.
        var occurrence = Cron.GetNextOccurrence(now.UtcDateTime.AddMinutes(-1), ScheduleTimeZone, inclusive: true);
        DateTime? last = null;
        while (occurrence.HasValue && occurrence.Value <= now.UtcDateTime)
        {
            last = occurrence;
            occurrence = Cron.GetNextOccurrence(occurrence.Value, ScheduleTimeZone);
        }
        return last.HasValue ? new DateTimeOffset(last.Value, TimeSpan.Zero) : null;
    }
}
