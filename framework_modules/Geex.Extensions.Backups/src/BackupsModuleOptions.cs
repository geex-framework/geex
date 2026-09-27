using MongoDB.Driver;

namespace Geex.Extensions.Backups;

public class BackupsModuleOptions : GeexModuleOptions
{
    public BackupsModuleOptions() => Enabled = false;

    public string WorkingDirectory { get; set; } = Path.Combine(GeexConstants.AppDataPath, "backup");
    public string MongodumpPath { get; set; } = "mongodump";
    public int RetentionCount { get; set; } = 7;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromHours(2);

    internal MongoUrl Validate(GeexCoreModuleOptions core)
    {
        if (Timeout <= TimeSpan.Zero || Timeout.TotalMilliseconds > uint.MaxValue - 1)
            throw new InvalidOperationException("Backup timeout must be positive and fit a cancellation timer.");
        if (string.IsNullOrWhiteSpace(MongodumpPath) || string.IsNullOrWhiteSpace(WorkingDirectory))
            throw new InvalidOperationException("Backup tool and working directory must be configured.");
        var url = new MongoUrl(core.ConnectionString);
        if (string.IsNullOrWhiteSpace(url.DatabaseName) || url.DatabaseName is "admin" or "config" or "local")
            throw new InvalidOperationException("Backup requires an explicit application database in GeexCoreModuleOptions.ConnectionString.");
        return url;
    }

    internal MongoUrl ValidateAutomatic(GeexCoreModuleOptions core)
    {
        if (RetentionCount < 1)
            throw new InvalidOperationException("Automatic backup retention must be positive.");
        return Validate(core);
    }
}
