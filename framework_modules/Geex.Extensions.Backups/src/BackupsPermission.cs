namespace Geex.Extensions.Backups;

public class BackupsPermission : AppPermission<BackupsPermission>
{
    public BackupsPermission(string value) : base($"Backups_{value}") { }
    public static BackupsPermission Query { get; } = new("query_backups");
    public static BackupsPermission Create { get; } = new("mutation_startBackup");
    public static BackupsPermission Expire { get; } = new("mutation_expireBackup");
}
