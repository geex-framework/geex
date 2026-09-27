using Geex.Extensions.Backups.Core;
using Geex.Extensions.Backups.Core.Jobs;
using Geex.Extensions.BackgroundJob;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Geex.Extensions.Backups.Tests;

public class BackupOptionsTests
{
    [Theory]
    [InlineData("mongodb://localhost:27017/")]
    [InlineData("mongodb://localhost:27017/admin")]
    [InlineData("mongodb://localhost:27017/config")]
    [InlineData("mongodb://localhost:27017/local")]
    public void RejectsMissingOrSystemDatabase(string uri)
    {
        Assert.Throws<InvalidOperationException>(() => new BackupsModuleOptions().Validate(
            new GeexCoreModuleOptions { ConnectionString = uri }));
    }

    [Fact]
    public void DefaultsAreOptInAndArgumentsAlwaysSelectOneDatabase()
    {
        var options = new BackupsModuleOptions();
        Assert.Equal("BackupsModuleOptions", options.BindSection);
        Assert.False(options.Enabled);
        Assert.Equal(7, options.RetentionCount);
        var start = MongodumpProcess.CreateStartInfo("mongodump", "pims", "directory with spaces/config.yml", "a.archive.gz");
        Assert.Equal(new[] { "--config", "directory with spaces/config.yml", "--db", "pims", "--archive=a.archive.gz", "--gzip" }, start.ArgumentList);
        Assert.False(start.UseShellExecute);
    }

    [Fact]
    public void UsesLocalTimeAndDoesNotReplayMissedDailySlot()
    {
        using var provider = new ServiceCollection().AddLogging().BuildServiceProvider();
        var job = new BackupsJob(provider, "0 0 3 * * *");
        Assert.Equal(TimeZoneInfo.Local, job.ScheduleTimeZone);
        var localScheduledAt = new DateTime(2026, 9, 21, 3, 0, 0, DateTimeKind.Unspecified);
        var expected = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localScheduledAt, TimeZoneInfo.Local));
        Assert.Equal(expected.UtcDateTime, job.Cron.GetNextOccurrence(expected.UtcDateTime.AddSeconds(-1), job.ScheduleTimeZone));
        Assert.Equal(expected, job.GetScheduledAt(expected));
        Assert.Equal(expected, job.GetScheduledAt(expected.AddSeconds(2)));
        Assert.Null(job.GetScheduledAt(expected.AddHours(1)));
        Assert.Null(job.GetScheduledAt(expected.AddSeconds(-1)));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void SwitchesOnlyControlCronRegistration(bool automaticEnabled, bool jobsDisabled)
    {
        var automatic = automaticEnabled && !jobsDisabled;
        var options = new BackupsModuleOptions
        {
            Enabled = automaticEnabled,
            RetentionCount = automatic ? 7 : 0
        };
        var jobs = new BackgroundJobModuleOptions { Disabled = jobsDisabled };
        if (automatic) jobs.JobConfigs[nameof(BackupsJob)] = "0 0 3 * * *";
        var services = new ServiceCollection().AddLogging().AddSingleton(options).AddSingleton(jobs)
            .AddSingleton(new GeexCoreModuleOptions { ConnectionString = "mongodb://localhost/business" });
        BackupsModule.RegisterJobs(services, options);
        using var provider = services.BuildServiceProvider();
        var hosted = provider.GetServices<IHostedService>().ToArray();
        Assert.Same(provider.GetRequiredService<BackupsExecution>(), Assert.Single(hosted.OfType<BackupsExecution>()));
        Assert.Equal(automatic ? 1 : 0, hosted.OfType<BackupsJob>().Count());
        Assert.NotNull(options.Validate(provider.GetRequiredService<GeexCoreModuleOptions>()));
    }
}
