using System.Security.Cryptography;
using System.Text;
using System.Globalization;
using Geex.Extensions.Backups.Core.Entities;
using MongoDB.Bson;

namespace Geex.Extensions.Backups.Core;

public sealed record ManualBackupExecution
{
    public ManualBackupExecution(string executionKey, string databaseName, string requestedByUserId, DateTimeOffset scheduledAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executionKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);
        if (executionKey.Length > 256) throw new ArgumentOutOfRangeException(nameof(executionKey));
        if (!ObjectId.TryParse(requestedByUserId, out _)) throw new ArgumentException("A valid result recipient is required.", nameof(requestedByUserId));
        if (scheduledAt == default) throw new ArgumentOutOfRangeException(nameof(scheduledAt));
        ExecutionKey = executionKey;
        DatabaseName = databaseName;
        RequestedByUserId = requestedByUserId;
        ScheduledAt = DateTimeOffset.FromUnixTimeMilliseconds(scheduledAt.ToUnixTimeMilliseconds());
    }

    public string ExecutionKey { get; }
    public string DatabaseName { get; }
    public string RequestedByUserId { get; }
    public DateTimeOffset ScheduledAt { get; }
    public string BackupId => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        DatabaseName.Length.ToString(CultureInfo.InvariantCulture) + ":" + DatabaseName + ExecutionKey)).AsSpan(0, 12)).ToLowerInvariant();

    public bool Matches(Backup backup) => backup.Id == BackupId && backup.ExecutionKey == ExecutionKey &&
        backup.DatabaseName == DatabaseName && backup.Source == BackupSource.Manual &&
        backup.RequestedByUserId == RequestedByUserId && backup.ScheduledAt == ScheduledAt;

    internal void EnsureMatches(Backup backup)
    {
        if (!Matches(backup)) throw new InvalidOperationException("Backup execution identity conflicts with an existing record.");
    }
}
