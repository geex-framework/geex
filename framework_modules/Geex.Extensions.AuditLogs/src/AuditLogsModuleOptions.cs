namespace Geex.Extensions.AuditLogs
{
    public class AuditLogsModuleOptions : GeexModuleOptions<AuditLogsModule>
    {
        public int RetentionDays { get; set; } = 365;
    }
}
