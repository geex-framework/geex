namespace Geex.Extensions.AuditLogs;

public class AuditLogsPermission : AppPermission<AuditLogsPermission>
{
    public AuditLogsPermission(string value) : base($"{nameof(AuditLogs)}_{value}")
    {
    }

    public static AuditLogsPermission Query { get; } = new("query_auditLogs");
    public static AuditLogsPermission Delete { get; } = new("mutation_deleteAuditLogs");
}
