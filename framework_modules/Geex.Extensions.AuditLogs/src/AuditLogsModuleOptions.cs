namespace Geex.Extensions.AuditLogs
{
    public class AuditLogsModuleOptions : GeexModuleOptions<AuditLogsModule>
    {
        public AuditLevel DefaultAuditLevel { get; set; } = AuditLevel.None;
        public int RetentionDays { get; set; } = 365;
    }
}
