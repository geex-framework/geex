namespace Geex.Extensions.Backups.Core;

public interface IBackupArchiveExporter
{
    /// <summary>Writes a non-empty archive, or throws on failure or cancellation. The caller owns backup state and storage.</summary>
    Task ExportAsync(string executable, string connectionString, string database, string directory, string archive,
        CancellationToken cancellationToken);
}
